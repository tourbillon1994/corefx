// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.IO;
using System.Threading;
using Xunit;

namespace System.Diagnostics.ProcessTests
{
    public partial class ProcessTest
    {
        Process CreateProcessError()
        {
            return CreateProcess("error");
        }

        Process CreateProcessInput()
        {
            return CreateProcess("input");
        }

        Process CreateProcessStream()
        {
            return CreateProcess("stream");
        }

        Process CreateProcessByteAtATime()
        {
            return CreateProcess("byteAtATime");
        }

        [Fact, ActiveIssue(1538, PlatformID.OSX)]
        public void Process_SyncErrorStream()
        {
            Process p = CreateProcessError();
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            string expected = TestExeName + " started error stream" + Environment.NewLine +
                              TestExeName + " closed error stream" + Environment.NewLine;
            Assert.Equal(expected, p.StandardError.ReadToEnd());
            Assert.True(p.WaitForExit(WaitInMS));
        }

        [Fact, ActiveIssue(1538, PlatformID.OSX)]
        public void Process_AsyncErrorStream()
        {
            for (int i = 0; i < 2; ++i)
            {
                StringBuilder sb = new StringBuilder();
                Process p = CreateProcessError();
                p.StartInfo.RedirectStandardError = true;
                p.ErrorDataReceived += (s, e) =>
                {
                    sb.Append(e.Data);
                    if (i == 1)
                    {
                        ((Process)s).CancelErrorRead();
                    }
                };
                p.Start();
                p.BeginErrorReadLine();

                if (p.WaitForExit(WaitInMS))
                    p.WaitForExit(); // This ensures async event handlers are finished processing.

                string expected = TestExeName + " started error stream" + (i == 1 ? "" : TestExeName + " closed error stream");
                Assert.Equal(expected, sb.ToString());
            }
        }

        [Fact, ActiveIssue(1538, PlatformID.OSX)]
        public void Process_SyncOutputStream()
        {
            Process p = CreateProcessStream();
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            string s = p.StandardOutput.ReadToEnd();
            Assert.True(p.WaitForExit(WaitInMS));
            Assert.Equal(TestExeName + " started" + Environment.NewLine + TestExeName + " closed" + Environment.NewLine, s);
        }

        [Fact, ActiveIssue(1538, PlatformID.OSX)]
        public void Process_AsyncOutputStream()
        {
            for (int i = 0; i < 2; ++i)
            {
                StringBuilder sb = new StringBuilder();
                Process p = CreateProcessStream();
                p.StartInfo.RedirectStandardOutput = true;
                p.OutputDataReceived += (s, e) =>
                {
                    sb.Append(e.Data);
                    if (i == 1)
                    {
                        ((Process)s).CancelOutputRead();
                    }
                };
                p.Start();
                p.BeginOutputReadLine();
                if (p.WaitForExit(WaitInMS))
                    p.WaitForExit(); // This ensures async event handlers are finished processing.

                string expected = TestExeName + " started" + (i == 1 ? "" : TestExeName + " closed");
                Assert.Equal(expected, sb.ToString());
            }
        }

        [Fact]
        public void Process_SyncStreams()
        {
            const string expected = "This string should come as output";
            Process p = CreateProcessInput();
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += (s, e) => { Assert.Equal(expected, e.Data); };
            p.Start();
            using (StreamWriter writer = p.StandardInput)
            {
                writer.WriteLine(expected);
            }
            Assert.True(p.WaitForExit(WaitInMS));
        }

        [Fact]
        public void Process_AsyncHalfCharacterAtATime()
        {
            var receivedOutput = false;
            Process p = CreateProcessByteAtATime();
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.StandardOutputEncoding = Encoding.Unicode;
            p.OutputDataReceived += (s, e) =>
            {
                Assert.Equal(e.Data, "a");
                receivedOutput = true;
            };
            p.Start();
            p.BeginOutputReadLine();

            if (p.WaitForExit(WaitInMS))
                p.WaitForExit(); // This ensures async event handlers are finished processing.

            Assert.True(receivedOutput);
        }

        [Fact]
        public void Process_StreamNegativeTests()
        {
            {
                Process p = new Process();
                Assert.Throws<InvalidOperationException>(() => p.StandardOutput);
                Assert.Throws<InvalidOperationException>(() => p.StandardError);
                Assert.Throws<InvalidOperationException>(() => p.BeginOutputReadLine());
                Assert.Throws<InvalidOperationException>(() => p.BeginErrorReadLine());
                Assert.Throws<InvalidOperationException>(() => p.CancelOutputRead());
                Assert.Throws<InvalidOperationException>(() => p.CancelErrorRead());
            }

            {
                Process p = CreateProcessStream();
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.OutputDataReceived += (s, e) => {};
                p.ErrorDataReceived += (s, e) => {};

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                Assert.Throws<InvalidOperationException>(() => p.StandardOutput);
                Assert.Throws<InvalidOperationException>(() => p.StandardError);
                Assert.True(p.WaitForExit(WaitInMS));
            }

            {
                Process p = CreateProcessStream();
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.OutputDataReceived += (s, e) => {};
                p.ErrorDataReceived += (s, e) => {};

                p.Start();

                StreamReader output = p.StandardOutput;
                StreamReader error = p.StandardError;

                Assert.Throws<InvalidOperationException>(() => p.BeginOutputReadLine());
                Assert.Throws<InvalidOperationException>(() => p.BeginErrorReadLine());
                Assert.True(p.WaitForExit(WaitInMS));
            }
        }
    }
}
