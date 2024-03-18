using System;

namespace ProtobufCompatibility
{
    using System.IO;
    using System.Linq;

    using Google.Protobuf;
    using ProtobufCompat;

    class ConsoleOutputStream : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush() {}

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Console.Write("{0:X2} ", buffer[i]);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var s = new CodedOutputStream(new ConsoleOutputStream());

            var bm = new BooleanMessage
            {
                BoolField = true,
            };

            Console.WriteLine("BooleanMessage: ");
            bm.WriteTo(s); s.Flush(); Console.WriteLine();

            var nm = new IntegerMessage
            {
                Int32Field = 1,
            };

            Console.WriteLine("IntegerMessage: ");
            nm.WriteTo(s); s.Flush(); Console.WriteLine();

            var bnm = new BnMessage
            {
                BoolField = true,
                Int32Field = 0,
            };

            Console.WriteLine("BnMessage: ");
            bnm.WriteTo(s); s.Flush(); Console.WriteLine();

            var bn2m = Bn2Message.Parser.ParseFrom(
                new BooleanMessage
                {
                    BoolField = true,
                }.ToByteArray());
            Console.WriteLine("Convert BooleanMessage to Bn2Message: {0}", bn2m);

            bm = BooleanMessage.Parser.ParseFrom(
                new Bn2Message
                {
                    Int32Field = 100,
                }.ToByteArray());
            Console.WriteLine("Convert Bn2Message to BooleanMessage: {0}", bm);

            nm = IntegerMessage.Parser.ParseFrom(
                new BooleanMessage
                {
                    BoolField = true,
                }.ToByteArray());
            Console.WriteLine("Convert BooleanMessage to IntegerMessage: {0}", nm);

            bm = BooleanMessage.Parser.ParseFrom(
                new IntegerMessage
                {
                    Int32Field = 200000,
                }.ToByteArray());
            Console.WriteLine("Convert IntegerMessage to BooleanMessage: {0}", bm);

            var n8m = N8Message.Parser.ParseFrom(
                new BooleanMessage
                {
                    BoolField = true,
                }.ToByteArray());
            Console.WriteLine("Convert BooleanMessage to N8Message: {0}", n8m);

            bm = BooleanMessage.Parser.ParseFrom(
                new N8Message
                {
                    Int64Field = 200000,
                }.ToByteArray());
            Console.WriteLine("Convert N8Message to BooleanMessage: {0}", bm);

            var bbm = new BytesMessage();
            var data = Enumerable.Range(0, 1024 * 10).Select(x => (byte)x).ToArray();
            bbm.Data = ByteString.CopyFrom(data, 0, data.Length);
            bbm.WriteTo(s); s.Flush(); Console.WriteLine();
            Console.WriteLine("Length before serialization and deserliazation: {0}", bbm.Data.Length);

            var bbma = bbm.ToByteArray();
            bbm = BytesMessage.Parser.ParseFrom(bbma);
            bbm.WriteTo(s); s.Flush(); Console.WriteLine();
            Console.WriteLine("Length after serialization and deserliazation: {0}", bbm.Data.Length);
        }
    }
}
