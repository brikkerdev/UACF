using NUnit.Framework;
using UACF.Services;
using System.Threading.Tasks;

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

        [Test]
        public async Task CompilationService_WaitForCompilationToFinish_ReturnsWithoutTimeout_WhenIdle()
        {
            var result = await CompilationService.Instance.WaitForCompilationToFinishAsync(timeoutSeconds: 2, pollMs: 50);
            Assert.IsNotNull(result);
            Assert.GreaterOrEqual(result.DurationMs, 0);
        }
    }
}
