using Xunit;

namespace CodeVerifier.Tests
{
    public class ExtendedInterfaceVerifierTest
    {
        private AssemblyGroup GetAssemblyGroup()
        {
            return new AssemblyGroup(typeof(ExtendedInterfaceVerifierTest).Assembly.Location);
        }

        private ExtendedInterfaceVerifier CreateVerifierAndTest(string testClassName)
        {
            var asmGroup = GetAssemblyGroup();
            var verifier = new ExtendedInterfaceVerifier(new Options());
            verifier.Verify(asmGroup, "CodeVerifier.Tests." + testClassName);
            return verifier;
        }

        [Fact]
        public void Test_MethodCompleted_Succeed()
        {
            var verifier = CreateVerifierAndTest(nameof(ExtendedClass_MethodCompleted));
            Assert.Equal(1, verifier.VerifiedTypes.Count);
            Assert.Equal(0, verifier.Errors.Count);
        }

        [Fact]
        public void Test_MethodMissing_Fail()
        {
            var verifier = CreateVerifierAndTest(nameof(ExtendedClass_MethodMissing));
            Assert.Equal(1, verifier.VerifiedTypes.Count);
            Assert.Equal(
                new[] { "Cannot find handler for CodeVerifier.Tests.IFoo.Hello" },
                verifier.Errors);
        }

        [Fact]
        public void Test_MethoRedundant_Fail()
        {
            var verifier = CreateVerifierAndTest(nameof(ExtendedClass_MethodRedundant));
            Assert.Equal(1, verifier.VerifiedTypes.Count);
            Assert.Equal(
                new[] { "Unused extended handler: CodeVerifier.Tests.ExtendedClass_MethodRedundant.Bye" },
                verifier.Errors);
        }

        [Fact]
        public void Test_ExplicitMethodCompleted_Succeed()
        {
            var verifier = CreateVerifierAndTest(nameof(ExtendedClass_ExplicitMethodCompleted));
            Assert.Equal(1, verifier.VerifiedTypes.Count);
            Assert.Equal(0, verifier.Errors.Count);
        }
    }
}
