using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Text;
using System.Windows.Forms;


namespace PeepsCompress
{
    class YAY0 : SlidingWindowAlgorithm
    {
        public override byte[] compress(byte[] file, int offset)
        {
            throw new NotImplementedException();
        }

        public override byte[] decompress(BinaryReader br, int offset, FileStream inputFile)
        {
            List<byte> newFile = new List<byte>();

            string magicNumber = Encoding.ASCII.GetString(br.ReadBytes(4));

            if (magicNumber.ToLower() == "yay0")
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

                                byte byte1Upper = (byte)((byte1 & 0x0F));
                                byte byte1Lower = (byte)((byte1 & 0xF0) >> 4);

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

                                for (int j = 0; j < finalLength; j++) //add data for finalLength iterations
                                {
                                    newFile.Add(newFile[newFile.Count - finalOffset]); //add byte at offset (fileSize - finalOffset) to file
                                }

                                inputFile.Seek(currentOffset, SeekOrigin.Begin); //return to layout bits
                                
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
                MessageBox.Show("This is not a YAY0 file.");
                return null;
            }
        }

        public override byte[] compressInitialization(string path)
        {
            FileStream inputFile = File.Open(path, FileMode.Open);
            BigEndianBinaryReader br = new BigEndianBinaryReader(inputFile);
            byte[] file = br.ReadBytes((int)inputFile.Length);

            return compress(file, 0);
        }

        public override byte[] decompressInitialization(string path)
        {
            FileStream inputFile = File.Open(path, FileMode.Open);
            BigEndianBinaryReader br = new BigEndianBinaryReader(inputFile);
            byte[] file = br.ReadBytes((int)inputFile.Length);
            string yay0File = Encoding.ASCII.GetString(file);
            int offset = yay0File.IndexOf("Yay0", StringComparison.OrdinalIgnoreCase);
            inputFile.Position = offset;

            return decompress(br, offset, inputFile);
        }
    }
}
