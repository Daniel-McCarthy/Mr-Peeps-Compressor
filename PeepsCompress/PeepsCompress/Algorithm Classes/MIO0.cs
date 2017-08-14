using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using System.Windows.Forms;

namespace PeepsCompress
{
    class MIO0 : SlidingWindowAlgorithm
    {

        public override byte[] decompressInitialization(string path)
        {
            FileStream inputFile = File.Open(path, FileMode.Open);
            BigEndianBinaryReader br = new BigEndianBinaryReader(inputFile);
            byte[] file = br.ReadBytes((int)inputFile.Length);
            string mio0File = Encoding.ASCII.GetString(file);
            int offset = mio0File.IndexOf("MIO0");
            inputFile.Position = offset;

            //decompress(file);

            return decompress(br, offset, inputFile);
        }


        public override byte[] decompress(BinaryReader br, int offset, FileStream inputFile)
        {

            List<byte> newFile = new List<byte>();

            string magicNumber = Encoding.ASCII.GetString(br.ReadBytes(4));

            if (magicNumber == "MIO0")
            {
                int decompressedLength = br.ReadInt32();
                int compressedOffset = br.ReadInt32() + offset;
                int uncompressedOffset = br.ReadInt32() + offset;
                int currentOffset;

                try
                {

                    while (newFile.Count < decompressedLength)
                    {

                        byte bits = br.ReadByte(); //byte of layout bits
                        BitArray arrayOfBits = new BitArray(new byte[1] { bits });

                        for (int i = 7; i > -1 && (newFile.Count < decompressedLength); i--) //iterate through layout bits
                        {

                            if (arrayOfBits[i] == true)
                            {
                                //non-compressed
                                //add one byte from uncompressedOffset to newFile

                                currentOffset = (int)inputFile.Position;

                                inputFile.Seek(uncompressedOffset, SeekOrigin.Begin);

                                newFile.Add(br.ReadByte());
                                uncompressedOffset++;

                                inputFile.Seek(currentOffset, SeekOrigin.Begin);

                            }
                            else
                            {
                                //compressed
                                //read 2 bytes
                                //4 bits = length
                                //12 bits = offset

                                currentOffset = (int)inputFile.Position;
                                inputFile.Seek(compressedOffset, SeekOrigin.Begin);

                                byte byte1 = br.ReadByte();
                                byte byte2 = br.ReadByte();
                                compressedOffset += 2;

                                //Note: For Debugging, binary representations can be printed with:  Convert.ToString(numberVariable, 2);

                                byte byte1Upper = (byte)((byte1 & 0x0F));//offset bits
                                byte byte1Lower = (byte)((byte1 & 0xF0) >> 4); //length bits

                                int combinedOffset = ((byte1Upper << 8) | byte2);

                                int finalOffset = 1 + combinedOffset;
                                int finalLength = 3 + byte1Lower;

                                for (int k = 0; k < finalLength; k++) //add data for finalLength iterations
                                {
                                    newFile.Add(newFile[newFile.Count - finalOffset]); //add byte at offset (fileSize - finalOffset) to file
                                }

                                inputFile.Seek(currentOffset, SeekOrigin.Begin); //return to layout bits

                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                inputFile.Close();

                return newFile.ToArray();
            }
            else
            {
                MessageBox.Show("This is not an MIO0 file.");
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

                inputFile.Close();

                return compress(file, 0);
            }
            else
            {
                byte[] stringToFile = Encoding.ASCII.GetBytes(path);

                return compress(stringToFile, 0);
            }
        }

        public override byte[] compress(byte[] file, int offset)
        {
            List<byte> layoutBits = new List<byte>();
            List<byte> dictionary = new List<byte>();

            List<byte> uncompressedData = new List<byte>();
            List<int[]> compressedData = new List<int[]>();

            int maxDictionarySize = 4096;
            int maxMatchLength = 18;
            int minimumMatchSize = 2;
            int decompressedSize = 0;

            for (int i = 0; i < file.Length; i++)
            {
                if (dictionary.Contains(file[i]))
                {
                    //check for best match
                    int[] matches = findAllMatches(ref dictionary, file[i]);
                    int[] bestMatch = findLargestMatch(ref dictionary, matches, ref file, i, maxMatchLength);

                    if (bestMatch[1] > minimumMatchSize)
                    {
                        //add to compressedData
                        layoutBits.Add(0);
                        bestMatch[0] = dictionary.Count - bestMatch[0]; //sets offset in relation to end of dictionary

                        for (int j = 0; j < bestMatch[1]; j++)
                        {
                            dictionary.Add(file[i + j]);
                        }

                        i = i + bestMatch[1] - 1;

                        compressedData.Add(bestMatch);
                        decompressedSize += bestMatch[1];
                    }
                    else
                    {
                        //add to uncompressed data
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

            return buildMIO0CompressedBlock(ref layoutBits, ref uncompressedData, ref compressedData, decompressedSize, offset);
        }

        public byte[] buildMIO0CompressedBlock(ref List<byte> layoutBits, ref List<byte> uncompressedData, ref List<int[]> offsetLengthPairs, int decompressedSize, int offset)
        {
            List<byte> finalMIO0Block = new List<byte>();           //the final compressed file
            List<byte> layoutBytes = new List<byte>();              //holds the layout bits in byte form
            List<byte> compressedDataBytes = new List<byte>();      //holds length/offset in 2byte form

            int compressedOffset = 16 + offset; //header size
            int uncompressedOffset;

            //added magic number
            finalMIO0Block.AddRange(Encoding.ASCII.GetBytes("MIO0")); //4 byte magic number

            //add decompressed data size
            byte[] decompressedSizeArray = BitConverter.GetBytes(decompressedSize);
            Array.Reverse(decompressedSizeArray);
            finalMIO0Block.AddRange(decompressedSizeArray);         //4 byte decompressed size

            //assemble layout bits into bytes
            while (layoutBits.Count > 0)                            //convert layout binary bits to bytes
            {
                //pad bits to full byte if necessary
                while (layoutBits.Count < 8)                         //pad last byte if necessary
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
            

            foreach (int[] offsetLengthPair in offsetLengthPairs)
            {
                offsetLengthPair[0] -= 1;                           //removes '1' that is added to offset on decompression
                offsetLengthPair[1] -= 3;                           //removes '3' that is added to length on decompression

                //combine offset and length into 16 bit block
                 int compressedInt = (offsetLengthPair[1] << 12) | (offsetLengthPair[0]);

                //split int16 into two bytes to be written
                byte[] compressed2Byte = new byte[2];
                compressed2Byte[0] = (byte)(compressedInt & 0xFF);
                compressed2Byte[1] = (byte)((compressedInt >> 8) & 0xFF);

                compressedDataBytes.Add(compressed2Byte[1]);        //used to be 0 then 1, but this seems to be correct
                compressedDataBytes.Add(compressed2Byte[0]);

            }

            //pad layout bits if needed
            while (layoutBytes.Count % 4 != 0)
            {
                layoutBytes.Add(0);
            }

            compressedOffset += layoutBytes.Count;

            //add final compressed offset
            byte[] compressedOffsetArray = BitConverter.GetBytes(compressedOffset);
            Array.Reverse(compressedOffsetArray);
            finalMIO0Block.AddRange(compressedOffsetArray);

            //add final uncompressed offset
            uncompressedOffset = compressedOffset + compressedDataBytes.Count;
            byte[] uncompressedOffsetArray = BitConverter.GetBytes(uncompressedOffset);
            Array.Reverse(uncompressedOffsetArray);
            finalMIO0Block.AddRange(uncompressedOffsetArray);

            //add layout bits
            foreach (byte layoutByte in layoutBytes)                 //add layout bytes to file
            {
                finalMIO0Block.Add(layoutByte);
            }

            //add compressed data
            foreach (byte compressedByte in compressedDataBytes)     //add compressed bytes to file
            {
                finalMIO0Block.Add(compressedByte);
            }

            //add uncompressed data
            foreach (byte uncompressedByte in uncompressedData)      //add noncompressed bytes to file
            {
                finalMIO0Block.Add(uncompressedByte);
            }

            return finalMIO0Block.ToArray();
        }



    }

}

