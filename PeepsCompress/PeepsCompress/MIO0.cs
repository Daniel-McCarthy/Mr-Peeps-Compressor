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

            /*
            if (file.Length == mio0File.Length)
            {
                MessageBox.Show("String and file same size");
            }
            else
            {
                MessageBox.Show("Stop: Not the same size. This will not work.");
            }
            

            while (mio0File.Contains("MIO0"))
            {
                int offset = mio0File.IndexOf("MIO0");
                inputFile.Seek(offset, SeekOrigin.Begin);

                byte[] decompressedFile = decompress(br, offset, inputFile);
                mio0File = Encoding.ASCII.GetString(file);
            }
            */

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
                                                                                             //for (int i = 0; i < 8; i++)
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

                return newFile.ToArray();
            }
            else
            {
                MessageBox.Show("This is not an MIO0 file.");
                return null;
            }
        }


        public override byte[] compressInitialization(string path)
        {
            return null;
        }

        public override byte[] compress(byte[] file, int offset)
        {
            return null;
        }

    }

}

