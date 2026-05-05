using System;
using IndustrialProcessing.Tasks;
using Xunit;

namespace IndustrialProcessing.Tests
{
    public class PayloadTests
    {
        [Fact]
        public void ToMap_ParsiraKljucVrijednost()
        {
            var map = PayloadDecoder.ToMap("numbers:12345,threads:4");
            Assert.Equal("12345", map["numbers"]);
            Assert.Equal("4", map["threads"]);
        }

        [Fact]
        public void ToMap_RadiCaseInsensitive()
        {
            var map = PayloadDecoder.ToMap("Numbers:10,Threads:2");
            Assert.Equal("10", map["numbers"]);
        }

        [Fact]
        public void ToMap_BacaNaLosFormat()
        {
            Assert.Throws<FormatException>(() => PayloadDecoder.ToMap("samokljuc"));
        }

        [Fact]
        public void ReadInteger_DozvoljavaUnderscore()
        {
            var map = PayloadDecoder.ToMap("numbers:10_000,threads:4");
            Assert.Equal(10000, PayloadDecoder.ReadInteger(map, "numbers"));
        }
    }
}
