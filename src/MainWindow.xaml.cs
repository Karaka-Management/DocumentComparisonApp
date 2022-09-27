using DocumentComparisonApp.Views;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using UglyToad.PdfPig;

namespace DocumentComparisonApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            #if OMS_DEMO
                this.Title = "Demo " + this.Title;
                MessageBox.Show("This is a demo with limited functionality.");
            #endif
        }

        private static string loadTextFile(string path)
        {
            return System.IO.File.ReadAllText(path).Trim();
        }

        private static string loadPdfFile(string path)
        {
            PdfDocument doc = PdfDocument.Open(path);

            string content = "";

            foreach (UglyToad.PdfPig.Content.Page page in doc.GetPages()) {
                content += " " + page.GetWords();
            }

            return content.Trim();
        }

        private static string loadWordFile(string path)
        {
            WordprocessingDocument wordDocument = WordprocessingDocument.Open(path, false);
            if (wordDocument.MainDocumentPart == null
                || wordDocument.MainDocumentPart.Document.Body == null
            ) {
                return "";
            }

            return wordDocument.MainDocumentPart.Document.Body.InnerText.Trim();
        }

        private string loadFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Text files|*.*"
            };

            if (openFileDialog.ShowDialog() != true) {
                return "";
            }

            switch (System.IO.Path.GetExtension(openFileDialog.FileName.ToLower()))
            {
                case ".doc":
                case ".docx":
                    return loadWordFile(openFileDialog.FileName);
                case ".pdf":
                    return loadPdfFile(openFileDialog.FileName);
                default:
                    return loadTextFile(openFileDialog.FileName);
            }
        }

        private void btnLoadFile1_Click(object sender, RoutedEventArgs e)
        {
            txtLeft.Document.Blocks.Clear();
            txtLeft.Document.Blocks.Add(new Paragraph(new Run(this.loadFile())));
        }

        private void btnLoadFile2_Click(object sender, RoutedEventArgs e)
        {
            txtRight.Document.Blocks.Clear();
            txtRight.Document.Blocks.Add(new Paragraph(new Run(this.loadFile())));
        }

        private void btnCompareText_Click(object sender, RoutedEventArgs e)
        {
            string[] arr1 = (new TextRange(txtLeft.Document.ContentStart, txtLeft.Document.ContentEnd)).Text.Split(" ");
            string[] arr2 = (new TextRange(txtRight.Document.ContentStart, txtRight.Document.ContentEnd)).Text.Split(" ");

            (string[] values, int[] masks) diff = this.computeLCSDiff(arr1, arr2);

            int start = 0;
            for (int i = 0; i < diff.values.Length; ++i) {
                if (diff.values[i] != null) {
                    break;
                }

                ++start;
            }

            string[] texts = diff.values[start..diff.values.Length];
            int[] masks    = diff.masks[start..diff.masks.Length];

            txtLeft.Document.Blocks.Clear();
            txtRight.Document.Blocks.Clear();
            Paragraph paraLeft  = new Paragraph();
            Paragraph paraRight = new Paragraph();

            string block = "";
            int status   = 0;

            int added   = 0;
            int removed = 0;

            Run runLeft  = new Run();
            Run runRight = new Run();

            int limit = texts.Length;
            #if OMS_DEMO
                limit = 100;
            #endif

            for (int i = 0; i < limit; ++i) {
                if (masks[i] != status) {
                    runLeft  = new Run(block);
                    runRight = new Run(block);

                    if (status == -1) {
                        ++removed;
                        runLeft.Background = new SolidColorBrush(Color.FromRgb(0xff, 0, 0));
                        paraLeft.Inlines.Add(runLeft);
                    } else if (status == 1) {
                        ++added;
                        runRight.Background = new SolidColorBrush(Color.FromRgb(0, 0xff, 0));
                        paraRight.Inlines.Add(runRight);
                    } else {
                        paraLeft.Inlines.Add(runLeft);
                        paraRight.Inlines.Add(runRight);
                    }

                    block  = "";
                    status = masks[i];
                }

                block += " " + texts[i];
            }

            runLeft  = new Run(block);
            runRight = new Run(block);
            if (status == -1) {
                ++removed;
                runLeft.Background = new SolidColorBrush(Color.FromRgb(0xff, 0, 0));
                paraLeft.Inlines.Add(runLeft);
            } else if (status == 1) {
                ++added;
                runRight.Background = new SolidColorBrush(Color.FromRgb(0, 0xff, 0));
                paraRight.Inlines.Add(runRight);
            } else {
                paraLeft.Inlines.Add(runLeft);
                paraRight.Inlines.Add(runRight);
            }

            txtLeft.Document.Blocks.Add(paraLeft);
            txtRight.Document.Blocks.Add(paraRight);

            statTextBlock.Text = "Words: " + Math.Max(arr1.Length, arr2.Length)
                + " Added: " + added
                + " Deleted: " + removed;
        }

        private (string[] values, int[] masks) computeLCSDiff(string[] from, string[] to)
        {
            string[] diffValues = new string[from.Length + to.Length];
            int[] diffMasks     = new int[from.Length + to.Length];

            int n1 = from.Length;
            int n2 = to.Length;

            int[][] dm = new int[n1 + 1][];
            dm[0]      = new int[n2 + 1];

            int i = 0;
            int j = 0;

            for (j = 0; j <= n2; ++j) {
                dm[0][j] = 0;
            }

            for (i = 0; i <= n1; ++i) {
                dm[i] = new int[n2 + 1];
                dm[i][0] = 0;
            }

            for (i = 1; i <= n1; ++i) {
                for (j = 1; j <= n2; ++j) {
                    dm[i][j] = from[i - 1] == to[j - 1]
                        ? dm[i - 1][j - 1] + 1
                        : Math.Max(dm[i - 1][j], dm[i][j - 1]);
                }
            }

            int diffIndex = 0;

            i = n1;
            j = n2;
            while (i > 0 || j > 0) {
                if (j > 0 && dm[i][j - 1] == dm[i][j]) {
                    diffValues[diffIndex] = to[j - 1];
                    diffMasks[diffIndex]  = 1;

                    --j;
                    ++diffIndex;

                    continue;
                }

                if (i > 0 && dm[i - 1][j] == dm[i][j]) {
                    diffValues[diffIndex] = from[i - 1];
                    diffMasks[diffIndex]  = -1;

                    --i;
                    ++diffIndex;

                    continue;
                }

                diffValues[diffIndex] = from[i - 1];
                diffMasks[diffIndex]  = 0;

                --i;
                --j;
                ++diffIndex;
            }

            Array.Reverse(diffValues);
            Array.Reverse(diffMasks);

            return (values: diffValues, masks: diffMasks);
        }

        // Remark: Not used, but could be used as alternative.
        private object[] diffStrings(string[] from, string[] to, int i = 0, int j = 0)
        {
            int N = from.Length;
            int M = to.Length;
            int L = N + M;
            int Z = 2 * Math.Min(N, M) + 2;

            if (N > 0 && M > 0)
            {
                int w = N - M;

                int[] g = new int[Z];
                Array.Clear(g, 0, Z);

                int[] p = new int[Z];
                Array.Clear(p, 0, Z);

                for (int h = 0; h < Convert.ToInt32(Math.Floor((double) (L / 2))) + (modulo(L, 2) != 0 ? 1 : 0) + 1; ++h) {
                    for (int r = 0; r < 2; ++r)
                    {
                        int[] c = g;
                        int[] d = p;

                        int o = 1;
                        int m = 1;

                        if (r != 0)
                        {
                            o = 0;
                            m = -1;

                            c = p;
                            d = g;
                        }

                        for (int k = -(h - 2 * Math.Max(0, h - M)); k < h - 2 * Math.Max(0, h - N) + 1; k += 2) {
                            int a = 0;
                            
                            if (k == -h || k != h && c[modulo(k - 1, Z)] < c[modulo(k + 1, Z)])
                            {
                                a = c[modulo(k + 1, Z)];
                            } else
                            {
                                a = c[modulo(k - 1, Z)] + 1;
                            }

                            int b = a - k;
                            int s = a;
                            int t = b;

                            while (a < N && b < M && from[(1 - o) * N + m * a + (o - 1)] == to[(1 - o) * M + m * b + (o - 1)])
                            {
                                ++a;
                                ++b;
                            }

                            c[modulo(k, Z)] = a;
                            int z = -(k - w);

                            if (modulo(L, 2) == o && z >= -(h - o) && z <= h - o && c[modulo(k, Z)] + d[modulo(z, Z)] >= N)
                            {
                                int D = 2 * h - 1;
                                int x = s;
                                int y = t;
                                int u = a;
                                int v = b;

                                if (o != 1)
                                {
                                    D = 2 * h;
                                    x = N - a;
                                    y = M - b;
                                    u = N - s;
                                    v = M - t;
                                }

                                if (D > 1 || (x != u && y != v))
                                {
                                    object[] o1 = diffStrings(from[0..x], to[0..y], i, j);
                                    object[] o2 = diffStrings(from[u..N], to[v..M], i + u, j + v);

                                    object[] combined = new object[o1.Length + o2.Length];
                                    Array.Copy(o1, combined, o1.Length);
                                    Array.Copy(o2, 0, combined, o1.Length, o2.Length);

                                    return combined;
                                }
                                else if (M > N)
                                {
                                    return diffStrings(new string[] { }, to[N..M], i + N, j + N);
                                }
                                else if (M < N)
                                {
                                    return diffStrings(from[M..N], new string[] { }, i + M, j + M);
                                }
                                else
                                {
                                    return new object[] { };
                                }
                            }
                        }
                    }
                }
            } else if (N > 0)
            {
                object[] diffs = new object[N];

                for (int n = 0; n < N; ++n)
                {
                    diffs[n] = new { operation = -1, posOld = i + n, posNew = -1 };
                }

                return diffs;
            } else
            {
                object[] diffs = new object[M];

                for (int n = 0; n < M; ++n)
                {
                    diffs[n] = new { operation = +1, posOld = i, posNew = j + n };
                }

                return diffs;
            }

            return new object[] { };
        }

        private static int modulo(int a, int b)
        {
            return (((a % b) + b) % b);
        }

        private void menuInfo_Click(object sender, RoutedEventArgs e)
        {
            if (Info.isOpen)
            {
                return;
            }

            Info window = new Info();
            window.Show();
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
            Environment.Exit(0);
        }
    }
}
