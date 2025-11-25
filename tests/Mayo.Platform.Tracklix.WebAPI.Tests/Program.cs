using Mayo.Platform.Tracklix.WebAPI.Tests.Integration;

namespace Mayo.Platform.Tracklix.WebAPI.Verification
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var verificationTests = new ManualVerificationTests();
            await verificationTests.RunAllVerifications();
        }
    }
}