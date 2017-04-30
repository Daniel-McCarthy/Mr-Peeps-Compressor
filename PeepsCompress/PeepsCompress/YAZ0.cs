using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using System.Windows.Forms;

namespace PeepsCompress
{
    class YAZ0 : RunLengthAlgorithm
    {
        public override byte[] compress(byte[] file, int offset)
        {
            return null;
        }
        public override byte[] decompress(BinaryReader br, int offset, FileStream inputFile)
        {
            List<byte> newFile = new List<byte>();
            //string fileString;

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
                                byte byte1 = br.ReadByte();
                                byte byte2 = br.ReadByte();

                                byte byte1Upper = (byte)((byte1 & 0x0F));
                                byte byte1Lower = (byte)((byte1 & 0xF0));
                                byte1Lower = (byte)(byte1Lower >> 4);

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

        public override byte[] compressInitialization(string path)
        {
            return null;
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
