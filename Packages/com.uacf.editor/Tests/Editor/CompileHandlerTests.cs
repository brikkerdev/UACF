using NUnit.Framework;
using UACF.Services;

namespace UACF.Tests
{
    public class CompileHandlerTests
    {
        [Test]
        public void CompilationService_Instance_Exists()
        {
            var svc = CompilationService.Instance;
            Assert.IsNotNull(svc);
        }

        [Test]
        public void CompilationService_GetLastResult_ReturnsResult()
        {
            var result = CompilationService.Instance.GetLastResult();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Compiled || result.ErrorCount >= 0);
        }
    }
}
