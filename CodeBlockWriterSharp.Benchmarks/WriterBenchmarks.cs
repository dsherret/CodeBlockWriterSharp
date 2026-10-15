using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace CodeBlockWriterSharp.Benchmarks
{
    [MemoryDiagnoser]
    public class WriterBenchmarks
    {
        private const int MethodCount = 200;

        private List<string> _lines;
        private string _multiLineText;
        private string _longQuoteText;

        [GlobalSetup]
        public void Setup()
        {
            _lines = new List<string>();
            var writer = new CodeBlockWriter();
            GenerateClient(writer);
            foreach (var line in writer.ToString().Split('\n'))
                _lines.Add(line.TrimStart());
            _multiLineText = string.Join("\n", _lines);

            var builder = new StringBuilder();
            for (var i = 0; i < 200; i++)
                builder.Append("Some \"quoted\" text that spans\na few lines. ");
            _longQuoteText = builder.ToString();
        }

        /// <summary>
        /// Generates code using the writer's API like a code generator would.
        /// </summary>
        [Benchmark(Baseline = true)]
        public string GenerateClient()
        {
            var writer = new CodeBlockWriter();
            GenerateClient(writer);
            return writer.ToString();
        }

        /// <summary>
        /// Writes code one line at a time.
        /// </summary>
        [Benchmark]
        public string WriteLines()
        {
            var writer = new CodeBlockWriter();
            foreach (var line in _lines)
                writer.WriteLine(line);
            return writer.ToString();
        }

        /// <summary>
        /// Writes code with many lines in a single write.
        /// </summary>
        [Benchmark]
        public string WriteMultiLineText()
        {
            var writer = new CodeBlockWriter();
            writer.Write("class Test").Block(() => writer.Write(_multiLineText));
            return writer.ToString();
        }

        /// <summary>
        /// Writes code in many small pieces.
        /// </summary>
        [Benchmark]
        public string WriteSmallPieces()
        {
            var writer = new CodeBlockWriter();
            for (var i = 0; i < MethodCount * 10; i++)
            {
                writer.Write("const").Space().Write("value").Write(i.ToString()).Space().Write("=").Space()
                    .Quote("text").Write(";").NewLine();
            }
            return writer.ToString();
        }

        /// <summary>
        /// Writes long text within quotes.
        /// </summary>
        [Benchmark]
        public string QuoteLongText()
        {
            var writer = new CodeBlockWriter();
            for (var i = 0; i < 20; i++)
                writer.Write("const text = ").Quote(_longQuoteText).Write(";").NewLine();
            return writer.ToString();
        }

        private static void GenerateClient(CodeBlockWriter writer)
        {
            writer.WriteLine("// deno-lint-ignore-file");
            writer.BlankLine();
            writer.Write("export class ApiClient").Block(() =>
            {
                writer.WriteLine("private _onUnauthorized: (() => void) | undefined;");
                writer.BlankLine();
                writer.Write("constructor(public readonly baseUrl: string)").Block();
                for (var i = 0; i < MethodCount; i++)
                {
                    writer.BlankLineIfLastNot();
                    writer.WriteLine("/**");
                    writer.WriteLine($" * Gets item {i}. It's the \"item\" endpoint.");
                    writer.WriteLine(" */");
                    writer.Write($"async getItem{i}(id: number, signal?: AbortSignal)").Block(() =>
                    {
                        writer.Write("let url = this.baseUrl + ").Quote($"/api/Items/GetItem{i}").Write(";").NewLine();
                        writer.WriteLine("const params = new URLSearchParams();");
                        writer.WriteLine("params.set(\"id\", \"\" + id);");
                        writer.WriteLine("url += \"?\" + params.toString();");
                        writer.WriteLine("const half = id / 2;");
                        writer.WriteLine("const match = /item=\"?([^\";]+)\"?/i.exec(url);");
                        writer.WriteLine("const response = await this.post(url, undefined, signal);");
                        writer.Write("if (response.status == 200)").Block(() =>
                        {
                            writer.WriteLine("return (await response.json()) as ItemView;");
                        });
                        writer.Write("else").Block(() =>
                        {
                            writer.WriteLine("throw new Error(`Failed getting ${id} at ${url}: ${response.status}`);");
                        });
                    });
                }
            });
            writer.BlankLine();
            writer.Write("export interface ItemView").Block(() =>
            {
                writer.WriteLine("id: number;");
                writer.WriteLine("name: string;");
                writer.Write("tags").Write(":").Space().Write("string[];").NewLine();
            });
        }
    }
}
