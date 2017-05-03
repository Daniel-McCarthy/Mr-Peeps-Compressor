using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            throw new NotImplementedException();
        }

        public override byte[] compressInitialization(string path)
        {
            throw new NotImplementedException();
        }

        public override byte[] decompressInitialization(string path)
        {
            throw new NotImplementedException();
        }
    }
}
