using System;
using System.Net;
using Xunit;
using GoToRelativeTime;

namespace GoToRelativeTime.Tests
{
    public class GoToRelativeTimeTest
    {
        [Fact]
        public void TestParseSeconds()
        {
            Assert.Equal(TimeSpan.FromSeconds(1), EntryPoint.ParseJump("1s"));
            Assert.Equal(TimeSpan.FromSeconds(1100), EntryPoint.ParseJump("1100s"));
            Assert.Equal(TimeSpan.FromSeconds(1100).Negate(), EntryPoint.ParseJump("-1100s"));
        }
        
        [Fact]
        public void TestParseHoursMinutesSecondsAndMillis()
        {
            Assert.Equal(TimeSpan.FromMilliseconds(1), EntryPoint.ParseJump(",1"));
            Assert.Equal(TimeSpan.FromMilliseconds(1100), EntryPoint.ParseJump("1,100"));
            Assert.Equal(TimeSpan.FromMilliseconds(1001), EntryPoint.ParseJump("1,1"));
            Assert.Equal(TimeSpan.FromSeconds(60).Negate(), EntryPoint.ParseJump("-1:"));
            Assert.Equal(TimeSpan.FromHours(1), EntryPoint.ParseJump("1::"));
            Assert.Equal(TimeSpan.FromMinutes(61), EntryPoint.ParseJump("1:1:"));
            Assert.Equal(TimeSpan.FromSeconds(3661), EntryPoint.ParseJump("1:1:1"));
            Assert.Equal(TimeSpan.FromMilliseconds(3600001), EntryPoint.ParseJump("1::,1"));
        }
        
        [Fact]
        public void TestParseJustMilliSeconds()
        {
            Assert.Equal(TimeSpan.FromMilliseconds(10), EntryPoint.ParseJump("10"));
        }

        [Fact]
        public void TestConvertTimespanToSamples()
        {
            Assert.Equal(48, EntryPoint.TimeSpanToSamples(48000, TimeSpan.FromMilliseconds(1)));
            Assert.Equal(48000, EntryPoint.TimeSpanToSamples(48000, TimeSpan.FromSeconds(1)));
            Assert.Equal(48048, EntryPoint.TimeSpanToSamples(48000, TimeSpan.FromMilliseconds(1001)));
        }
    }
}