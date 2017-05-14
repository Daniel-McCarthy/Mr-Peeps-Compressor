using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using System.Windows.Forms;

namespace PeepsCompress
{
    class YAZ0 : SlidingWindowAlgorithm
    {
        public override byte[] compress(byte[] file, int offset)
        {
            List<byte> layoutBits = new List<byte>();
            List<byte> dictionary = new List<byte>();

            List<byte> uncompressedData = new List<byte>();
            List<int[]> compressedData = new List<int[]>();

            int maxDictionarySize = 4096;
            int minMatchLength = 3;
            int maxMatchLength = 255 + 0x12;
            int decompressedSize = 0;

            for (int i = 0; i < file.Length; i++)
            {

                if (dictionary.Contains(file[i]))
                {
                    //compressed data
                    int[] matches = findAllMatches(ref dictionary, file[i]);
                    int[] bestMatch = findLargestMatch(ref dictionary, matches, ref file, i, maxMatchLength);

                    if(bestMatch[1] >= minMatchLength)
                    {
                        layoutBits.Add(0);
                        bestMatch[0] = dictionary.Count - bestMatch[0];

                        for(int j = 0; j < bestMatch[1]; j++)
                        {
                            dictionary.Add(file[i + j]);
                        }

                        i = i + bestMatch[1] - 1;

                        compressedData.Add(bestMatch);
                        decompressedSize += bestMatch[1];
                    }
                    else
                    {
                        //uncompressed data
                        layoutBits.Add(1);
                        uncompressedData.Add(file[i]);
                        dictionary.Add(file[i]);
                        decompressedSize++;
                    }
                }
                else
                {
                    //uncompressed data
                    layoutBits.Add(1);
                    uncompressedData.Add(file[i]);
                    dictionary.Add(file[i]);
                    decompressedSize++;
                }

                if (dictionary.Count > maxDictionarySize)
                {
                    int overflow = dictionary.Count - maxDictionarySize;
                    dictionary.RemoveRange(0, overflow);
                }
            }

            return buildYAZ0CompressedBlock(ref layoutBits, ref uncompressedData, ref compressedData, decompressedSize, offset);
        }

        public byte[] buildYAZ0CompressedBlock(ref List<byte> layoutBits, ref List<byte> uncompressedData, ref List<int[]> offsetLengthPairs, int decompressedSize, int offset)
        {
            List<byte> finalYAZ0Block = new List<byte>();
            List<byte> layoutBytes = new List<byte>();
            List<byte> compressedDataBytes = new List<byte>();
            List<byte> extendedLengthBytes = new List<byte>();

            //add Yaz0 magic number
            finalYAZ0Block.AddRange(Encoding.ASCII.GetBytes("Yaz0"));

            byte[] decompressedSizeArray = BitConverter.GetBytes(decompressedSize);
            Array.Reverse(decompressedSizeArray);
            finalYAZ0Block.AddRange(decompressedSizeArray);

            //add 8 0's per format specification
            for(int i = 0; i < 8; i++)
            {
                finalYAZ0Block.Add(0);
            }

            //assemble layout bytes
            while(layoutBits.Count > 0)
            {
                while(layoutBits.Count < 8)
                {
                    layoutBits.Add(0);
                }

                string layoutBitsString = layoutBits[0].ToString() + layoutBits[1].ToString() + layoutBits[2].ToString() + layoutBits[3].ToString()
                        + layoutBits[4].ToString() + layoutBits[5].ToString() + layoutBits[6].ToString() + layoutBits[7].ToString();

                byte[] layoutByteArray = new byte[1];
                layoutByteArray[0] = Convert.ToByte(layoutBitsString, 2);
                layoutBytes.Add(layoutByteArray[0]);
                layoutBits.RemoveRange(0, (layoutBits.Count < 8) ? layoutBits.Count : 8);

            }

            //assemble offsetLength shorts
            foreach(int[] offsetLengthPair in offsetLengthPairs)
            {
                //if < 18, set 4 bits -2 as matchLength
                //if >= 18, set matchLength == 0, write length to new byte - 0x12

                int adjustedOffset = offsetLengthPair[0];
                int adjustedLength = (offsetLengthPair[1] >= 18) ? 0 : offsetLengthPair[1] - 2; //vital, 4 bit range is 0-15. Number must be at least 3 (if 2, when -2 is done, it will think it is 3 byte format), -2 is how it can store up to 17 without an extra byte because +2 will be added on decompression

                if (adjustedLength == 0)
                {
                    extendedLengthBytes.Add((byte)(offsetLengthPair[1] - 18));
                }

                int compressedInt = ((adjustedLength << 12) | adjustedOffset - 1);

                byte[] compressed2Byte = new byte[2];
                compressed2Byte[0] = (byte)(compressedInt & 0XFF);
                compressed2Byte[1] = (byte)((compressedInt >> 8) & 0xFF);

                compressedDataBytes.Add(compressed2Byte[1]);
                compressedDataBytes.Add(compressed2Byte[0]);
            }

            //add rest of file
           for(int i = 0; i < layoutBytes.Count; i++)
            {
                finalYAZ0Block.Add(layoutBytes[i]);

                BitArray arrayOfBits = new BitArray(new byte[1] { layoutBytes[i] });

                for (int j = 7; j > -1 && finalYAZ0Block.Count < decompressedSize; j--)
                {
                    if(arrayOfBits[j] == true)
                    {
                        finalYAZ0Block.Add(uncompressedData[0]);
                        uncompressedData.RemoveAt(0);
                    }
                    else
                    {
                        if (compressedDataBytes.Count > 0)
                        {
                            int length = compressedDataBytes[0] >> 4;

                            finalYAZ0Block.Add(compressedDataBytes[0]);
                            finalYAZ0Block.Add(compressedDataBytes[1]);
                            compressedDataBytes.RemoveRange(0, 2);

                            if (length == 0)
                            {
                                finalYAZ0Block.Add(extendedLengthBytes[0]);
                                extendedLengthBytes.RemoveAt(0);
                            }
                        }
                    }
                }
                    

            }

            return finalYAZ0Block.ToArray();
        }
        public override byte[] decompress(BinaryReader br, int offset, FileStream inputFile)
        {
            List<byte> newFile = new List<byte>();

            string magicNumber = Encoding.ASCII.GetString(br.ReadBytes(4));

            if (magicNumber.ToLower() == "yaz0")
            {
                int decompressedLength = br.ReadInt32();
                inputFile.Seek(8, SeekOrigin.Current); //skip 0s
                try
                {
                    while (newFile.Count < decompressedLength)
                    {
                        byte bits = br.ReadByte();
                        BitArray arrayOfBits = new BitArray(new byte[1] { bits });

                        for (int i = 7; i > -1 && (newFile.Count < decompressedLength); i--)
                        {
                            if (arrayOfBits[i] == true)
                            {
                                newFile.Add(br.ReadByte());
                            }
                            else
                            {
                                //read 2 compressed bytes
                                byte byte1 = br.ReadByte();
                                byte byte2 = br.ReadByte();

                                //split first byte into two values
                                byte byte1Upper = (byte)((byte1 & 0x0F));
                                byte byte1Lower = (byte)((byte1 & 0xF0));
                                byte1Lower = (byte)(byte1Lower >> 4);

                                //combine offset
                                int finalOffset = ((byte1Upper << 8) | byte2) + 1;
                                int finalLength;

                                if (byte1Lower == 0)
                                {
                                    finalLength = br.ReadByte() + 0x12;
                                }
                                else
                                {
                                    finalLength = byte1Lower + 2;
                                }

                                for (int j = 0; j < finalLength; j++)
                                {
                                    newFile.Add(newFile[newFile.Count - finalOffset]);
                                }

                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                return newFile.ToArray();
            }
            else
            {
                MessageBox.Show("This is not a YAZ0 file.");
                return null;
            }
            
        }

        public override byte[] compressInitialization(string path, bool fileInputMode)
        {
            if (fileInputMode)
            {
                FileStream inputFile = File.Open(path, FileMode.Open);
                BigEndianBinaryReader br = new BigEndianBinaryReader(inputFile);
                byte[] file = br.ReadBytes((int)inputFile.Length);

                return compress(file, 0);
            }
            else
            {
                byte[] stringToFile = Encoding.ASCII.GetBytes(path);

                return compress(stringToFile, 0);
            }
        }
        public override byte[] decompressInitialization(string path)
        {
            FileStream inputFile = File.Open(path, FileMode.Open);
            BigEndianBinaryReader br = new BigEndianBinaryReader(inputFile);
            byte[] file = br.ReadBytes((int)inputFile.Length);
            string yaz0File = Encoding.ASCII.GetString(file);
            int offset = yaz0File.IndexOf("Yaz0", StringComparison.OrdinalIgnoreCase);
            inputFile.Position = offset;

            return decompress(br, offset, inputFile);
        }
    }
}
