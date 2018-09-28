using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using MachineLearning;

namespace MachineLearningTools {
    class Program {
        static void CheckPredictions(string predictionsPath, string answersPath, bool viewDiff, int skipLines = 0) {
            if (!File.Exists(predictionsPath) || !File.Exists(answersPath))
                throw new Exception("One of files does not exists");

            StreamReader predictions = new StreamReader(predictionsPath);
            StreamReader answers = new StreamReader(answersPath);

            for (int i = 0; i < skipLines; i++) {
                predictions.ReadLine();
                answers.ReadLine();
            }

            long total = 0;
            long correct = 0;
            int line = skipLines + 1;

            while (!predictions.EndOfStream && !answers.EndOfStream) {
                string prediction = predictions.ReadLine();
                string answer = answers.ReadLine();

                total++;

                if (prediction == answer) {
                    correct++;
                }
                else if (viewDiff) {
                    Console.WriteLine("Line {0}: prediction: {1}, answer: {2}", line, prediction, answer);
                }

                line++;
            }

            if (!predictions.EndOfStream || !answers.EndOfStream)
                throw new Exception("Files have different sizes!");

            predictions.Close();
            answers.Close();

            double accuracy = (double)correct / total;

            if (accuracy > 0.8) {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if (accuracy > 0.5) {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine("Accuracy: {0} ({1} / {2})", accuracy, correct, total);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        static void CreateAnswersByDirectories(string path, string answersPath, string headline = "") {
            string[] directories = Directory.GetDirectories(path);

            string[][] filePaths = new string[directories.Length][];

            for (int i = 0; i < directories.Length; i++) {
                filePaths[i] = Directory.GetFiles(directories[i]);

                int slashIndex = directories[i].LastIndexOf("\\");

                if (slashIndex > -1)
                    directories[i] = directories[i].Substring(slashIndex + 1);
            }

            StreamWriter answers = new StreamWriter(answersPath);

            if (headline.Length > 0)
                answers.WriteLine(headline);

            int index = 1;

            for (int folder = 0; folder < filePaths.Length; folder++) {
                for (int file = 0; file < filePaths[folder].Length; file++) {
                    string filePath = filePaths[folder][file];

                    answers.WriteLine("{0},{1}", index++, directories[folder]);
                }
            }

            answers.Close();
        }

        static void CreateTestSetByDirectories(string path, string testPath, bool asRGB, string headline = "") {
            string[] directories = Directory.GetDirectories(path);

            string[][] filePaths = new string[directories.Length][];

            for (int i = 0; i < directories.Length; i++)
                filePaths[i] = Directory.GetFiles(directories[i]);

            StreamWriter testSet = new StreamWriter(testPath);

            if (headline.Length > 0)
                testSet.WriteLine(headline);

            int dataHeight = 0;
            int dataWidth = 0;

            for (int folder = 0; folder < filePaths.Length; folder++) {
                for (int file = 0; file < filePaths[folder].Length; file++) {
                    string filePath = filePaths[folder][file];

                    Stream stream = new FileStream(filePath, FileMode.Open);
                    Bitmap bitmap = new Bitmap(stream);
                    stream.Close();
                    stream.Dispose();

                    if (dataWidth == 0 || dataHeight == 0) {
                        dataWidth = bitmap.Width;
                        dataHeight = bitmap.Height;

                        Console.WriteLine("Pictures size: {0} x {1}", dataWidth, dataHeight);
                    }

                    if (dataWidth != bitmap.Width || dataHeight != bitmap.Height)
                        throw new Exception("Images have different sizes!");

                    for (int i = 0; i < dataHeight; i++) {
                        for (int j = 0; j < dataWidth; j++) {
                            Color pixel = bitmap.GetPixel(j, i);

                            if (asRGB) {
                                testSet.Write("{0},{1},{2}", pixel.R, pixel.G, pixel.B);
                            }
                            else {
                                int brightness = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                                testSet.Write("{0}", brightness);
                            }

                            if (i != dataHeight - 1 || j != dataWidth - 1)
                                testSet.Write(",");
                        }
                    }

                    testSet.WriteLine();
                    Console.WriteLine("{0} file processed", filePath);
                }
            }

            testSet.Close();
        }

        static void CreateTrainSetByDirectories(string path, string trainPath, bool asRGB, string headline = "") {
            string[] directories = Directory.GetDirectories(path);

            string[][] filePaths = new string[directories.Length][];

            for (int i = 0; i < directories.Length; i++) {
                filePaths[i] = Directory.GetFiles(directories[i]);

                int slashIndex = directories[i].LastIndexOf("\\");

                if (slashIndex > -1)
                    directories[i] = directories[i].Substring(slashIndex + 1);
            }

            StreamWriter trainSet = new StreamWriter(trainPath);

            if (headline.Length > 0)
                trainSet.WriteLine(headline);

            int dataHeight = 0;
            int dataWidth = 0;

            List<string[]> paths = new List<string[]>();

            for (int folder = 0; folder < filePaths.Length; folder++)
                for (int file = 0; file < filePaths[folder].Length; file++)
                    paths.Add(new string[] { filePaths[folder][file], directories[folder] });

            Random random = new Random();

            while (paths.Count > 0) {
                int index = random.Next(paths.Count);
                string filePath = paths[index][0];

                Stream stream = new FileStream(filePath, FileMode.Open);
                Bitmap bitmap = new Bitmap(stream);
                stream.Close();
                stream.Dispose();

                if (dataWidth == 0 || dataHeight == 0) {
                    dataWidth = bitmap.Width;
                    dataHeight = bitmap.Height;

                    Console.WriteLine("Pictures size: {0} x {1}", dataWidth, dataHeight);
                }

                if (dataWidth != bitmap.Width || dataHeight != bitmap.Height)
                    throw new Exception("Images have different sizes!");

                trainSet.Write(paths[index][1]);

                for (int i = 0; i < dataHeight; i++) {
                    for (int j = 0; j < dataWidth; j++) {
                        Color pixel = bitmap.GetPixel(j, i);

                        if (asRGB) {
                            trainSet.Write(",{0},{1},{2}", pixel.R, pixel.G, pixel.B);
                        }
                        else {
                            int brightness = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                            trainSet.Write(",{0}", brightness);
                        }

                    }
                }

                trainSet.WriteLine();
                Console.WriteLine("{0} file processed", filePath);
                paths.RemoveAt(index);
            }

            trainSet.Close();
        }

        static void CreateTrainSetByFilesAndLabels(string path, string labelsPath, string trainPath, int skiplines, bool asRGB, string headline = "") {
            string[] fullFiles = Directory.GetFiles(path);
            string[] files = new string[fullFiles.Length];

            for (int i = 0; i < files.Length; i++) {
                int index = fullFiles[i].LastIndexOf("\\");

                if (index > -1) {
                    files[i] = fullFiles[i].Substring(index + 1);
                }
                else {
                    files[i] = fullFiles[i];
                }

                index = files[i].IndexOf(".");

                if (index > -1)
                    files[i] = files[i].Substring(0, index);
            }

            StreamReader labels = new StreamReader(labelsPath);
            StreamWriter trainSet = new StreamWriter(trainPath);

            for (int i = 0; i < skiplines; i++)
                labels.ReadLine();

            if (headline.Length > 0)
                trainSet.WriteLine(headline);

            int dataWidth = 0;
            int dataHeight = 0;

            while (!labels.EndOfStream) {
                string line = labels.ReadLine();
                string[] splited = line.Split(',');

                string fileName = splited[0];
                string className = splited[1];

                int index = 0;

                while (index < files.Length && files[index] != fileName)
                    index++;

                if (index == files.Length)
                    throw new Exception("Can't find file with name '" + fileName + "'");

                Stream stream = new FileStream(fullFiles[index], FileMode.Open);
                Bitmap bitmap = new Bitmap(stream);
                stream.Close();
                stream.Dispose();

                if (dataWidth == 0 || dataHeight == 0) {
                    dataWidth = bitmap.Width;
                    dataHeight = bitmap.Height;

                    Console.WriteLine("Pictures size: {0} x {1}", dataWidth, dataHeight);
                }

                if (dataWidth != bitmap.Width || dataHeight != bitmap.Height)
                    throw new Exception("Images have different sizes!");

                trainSet.Write(className);

                for (int i = 0; i < dataHeight; i++) {
                    for (int j = 0; j < dataWidth; j++) {
                        Color pixel = bitmap.GetPixel(j, i);

                        if (asRGB) {
                            trainSet.Write(",{0},{1},{2}", pixel.R, pixel.G, pixel.B);
                        }
                        else {
                            int brightness = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                            trainSet.Write(",{0}", brightness);
                        }

                    }
                }

                trainSet.WriteLine();
                Console.WriteLine("{0} file processed", fullFiles[index]);
            }

            labels.Close();
            trainSet.Close();
        }

        static void TrainNeuralNetwork(NeuralNetwork network, TrainParameters parameters, string trainPath, string[] classes, int skiplines) {
            NeuroStructure structure = network.GetStructure();

            Console.WriteLine();
            Console.WriteLine("Created network '{0}': ", structure.name);
            Console.Write("  Neurons: {0} - ", structure.inputs);

            for (int i = 0; i < structure.hiddens.Length; i++)
                Console.Write("{0} - ", structure.hiddens[i]);
            Console.WriteLine("{0}", structure.outputs);
            Console.WriteLine("  hiddens AF: {0}", structure.hiddensFunction.ToString());
            Console.WriteLine("  output AF: {0}", structure.outputFunction.ToString());
            Console.WriteLine();

            List<Vector> inputData = new List<Vector>();
            List<Vector> outputData = new List<Vector>();

            StreamReader reader = new StreamReader(trainPath);

            for (int i = 0; i < skiplines; i++)
                reader.ReadLine();

            while (!reader.EndOfStream) {
                string line = reader.ReadLine();
                string[] splited = line.Split(',');

                int index = Array.IndexOf(classes, splited[0]);

                if (index == -1)
                    throw new Exception("Create train vectors, can't find class '" + splited[0] + "' in classes");

                Vector output = new Vector(classes.Length);

                output[index] = 1;

                Vector input = new Vector(splited.Length - 1);

                for (int i = 0; i < input.Length; i++)
                    input[i] = 0.01 + double.Parse(splited[i + 1]) / 255 * 0.99;

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine("Create {0} input and output vectors of data", inputData.Count);
                inputData.Add(input);
                outputData.Add(output);
            }

            reader.Close();

            Console.WriteLine();
            Console.WriteLine("Starting train of network:");
            Console.WriteLine("  learning rate: {0}", parameters.learningRate);
            Console.WriteLine("  maximum epochs: {0}", parameters.maxEpochs);
            Console.WriteLine("  accuracy: {0}", parameters.accuracy);

            network.Train(inputData.ToArray(), outputData.ToArray(), parameters, (double error, long epoch) => { Console.WriteLine("Error: {0}, epoch: {1}", error, epoch); });
            network.Save(structure.name + ".txt");

            Console.WriteLine("Network saved as '" + structure.name + ".txt'");
        }

        static void PredictByNeuralNetwork(string networkPath, string testPath, int skiplines, string[] classes, string predictPath, string headline = "") {
            NeuralNetwork network = new NeuralNetwork(networkPath);
            StreamReader reader = new StreamReader(testPath);
            StreamWriter writer = new StreamWriter(predictPath);

            for (int i = 0; i < skiplines; i++)
                reader.ReadLine();

            if (headline.Length > 0)
                writer.WriteLine(headline);

            int rowNumber = 1;
            Console.WriteLine();
            while (!reader.EndOfStream) {
                string line = reader.ReadLine();
                string[] splited = line.Split(',');

                Vector input = new Vector(splited.Length);

                for (int i = 0; i < input.Length; i++)
                    input[i] = 0.01 + double.Parse(splited[i]) / 255 * 0.99; // 0.01 .. 1

                Vector output = network.GetOutput(input);

                int index = 0;

                for (int i = 1; i < output.Length; i++)
                    if (output[i] > output[index])
                        index = i;

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine("Classified row{0} ({1})", rowNumber, classes[index]);

                writer.WriteLine("{0},{1}", rowNumber, classes[index]);
                rowNumber++;
            }

            reader.Close();
            writer.Close();
        }

        static void PredictByKNN(string trainPath, string[] classes, int k, string testPath, int skiplines, string predictPath, KNearestNeighbors.MetricType type, string headline = "") {
            StreamReader train = new StreamReader(trainPath);

            for (int i = 0; i < skiplines; i++)
                train.ReadLine();

            List<Vector> inputData = new List<Vector>();
            List<int> outputData = new List<int>();

            Console.WriteLine("Read train data\n");
            int rowNumber = 1;

            while (!train.EndOfStream) {
                string line = train.ReadLine();
                string[] splited = line.Split(',');

                Vector input = new Vector(splited.Length - 1);
                int index = Array.IndexOf(classes, splited[0]);

                outputData.Add(index);

                for (int i = 0; i < input.Length; i++)
                    input[i] = 0.01 + double.Parse(splited[i + 1]) / 255 * 0.99;

                inputData.Add(input);

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine("Read train row{0}", rowNumber);
                rowNumber++;
            }

            KNearestNeighbors KNN = new KNearestNeighbors(inputData.ToArray(), outputData.ToArray(), classes.Length, k, typen);
            StreamReader reader = new StreamReader(testPath);
            StreamWriter writer = new StreamWriter(predictPath);

            for (int i = 0; i < skiplines; i++)
                reader.ReadLine();

            if (headline.Length > 0)
                writer.WriteLine(headline);

            rowNumber = 1;

            Console.WriteLine("Classifing:\n");

            while (!reader.EndOfStream) {
                string line = reader.ReadLine();
                string[] splited = line.Split(',');

                Vector input = new Vector(splited.Length);

                for (int i = 0; i < input.Length; i++)
                    input[i] = 0.01 + double.Parse(splited[i]) / 255 * 0.99; // 0.01 .. 1

                int index = KNN.GetClassIndex(input);

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine("Classified row{0} ({1})", rowNumber, classes[index]);
                writer.WriteLine("{0},{1}", rowNumber, classes[index]);
                rowNumber++;
            }

            reader.Close();
            writer.Close();
        }

        static void SplitToSets(string path, string trainPath, string testPath, string answersPath, string trainHeadline, string testHeadline, string answersHeadline, int skiplines, double fraction) {
            if (fraction < 0.05 || fraction > 0.95)
                throw new Exception("Fraction must be in [0.05, 0.95]");

            List<string> lines = new List<string>();
            StreamReader sr = new StreamReader(path);

            for (int i = 0; i < skiplines; i++)
                sr.ReadLine();

            while (!sr.EndOfStream)
                lines.Add(sr.ReadLine());

            Random random = new Random(DateTime.Now.Millisecond);
            lines = lines.OrderBy((x) => random.Next()).ToList();

            StreamWriter trainSet = new StreamWriter(trainPath);
            StreamWriter testSet = new StreamWriter(testPath);
            StreamWriter answers = new StreamWriter(answersPath);

            if (trainHeadline.Length > 0)
                trainSet.WriteLine(trainHeadline);

            if (testHeadline.Length > 0)
                testSet.WriteLine(testHeadline);

            if (answersHeadline.Length > 0)
                answers.WriteLine(answersHeadline);

            int testLength = (int)(lines.Count * fraction);
            int trainLength = lines.Count - testLength;

            Console.WriteLine("Write to train set:\n");
            for (int i = 0; i < trainLength; i++) {
                trainSet.WriteLine(lines[i]);

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine("Write row {0}", i + 1);
            }

            Console.WriteLine("Write to test set:\n");
            for (int i = 0; i < testLength; i++) {
                int index = lines[i + trainLength].IndexOf(",");

                testSet.WriteLine(lines[i + trainLength].Substring(index + 1));
                answers.WriteLine("{0},{1}", i + 1, lines[i + trainLength].Substring(0, index));

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine("Write row {0}", i + 1);
            }

            trainSet.Close();
            testSet.Close();
            answers.Close();
        }

        static int GetItem(int total) {
            string answer = Console.ReadLine();
            int item;

            while (!int.TryParse(answer, out item)) {
                Console.Write("Incorrect item. Try again: ");
                answer = Console.ReadLine();
            }

            return item;
        }

        static void CheckPredictions() {
            Console.Write("Enter path to predictions path: ");
            string predictionsPath = Console.ReadLine();
            Console.Write("Enter path to answers file: ");
            string answersPath = Console.ReadLine();
            Console.Write("View diff? (y/n): ");
            bool viewDiff = Console.ReadLine() == "y";
            Console.Write("Enter number of skipped lines (may be 0): ");
            int skiplines = int.Parse(Console.ReadLine());

            CheckPredictions(predictionsPath, answersPath, viewDiff, skiplines);
        }

        static void CreateAnswersByDirectories() {
            Console.Write("Enter path to directory with directories with files: ");
            string path = Console.ReadLine();
            Console.Write("Enter path to answers file: ");
            string answersPath = Console.ReadLine();
            Console.Write("Enter headline (may be empty): ");
            string headline = Console.ReadLine();

            CreateAnswersByDirectories(path, answersPath, headline);
        }

        static void CreateTestSetByDirectories() {
            Console.Write("Enter path to directory with directories with pictures: ");
            string path = Console.ReadLine();
            Console.Write("Enter path to test set file: ");
            string testSetPath = Console.ReadLine();
            Console.Write("Pictures as RGB (y/n): ");
            bool asRGB = Console.ReadLine() == "y";
            Console.Write("Enter headline (may be empty): ");
            string headline = Console.ReadLine();

            CreateTestSetByDirectories(path, testSetPath, asRGB, headline);
        }

        static void CreateTrainSetByDirectories() {
            Console.Write("Enter path to directory with directories with pictures: ");
            string path = Console.ReadLine();
            Console.Write("Enter path to train set file: ");
            string trainSetPath = Console.ReadLine();
            Console.Write("Pictures as RGB (y/n): ");
            bool asRGB = Console.ReadLine() == "y";
            Console.Write("Enter headline (may be empty): ");
            string headline = Console.ReadLine();

            CreateTrainSetByDirectories(path, trainSetPath, asRGB, headline);
        }

        static void CreateTrainSetByFilesAndLabels() {
            Console.Write("Enter path to directory with pictures: ");
            string path = Console.ReadLine();
            Console.Write("Enter path to labels file: ");
            string labelsPath = Console.ReadLine();
            Console.Write("Enter number of skipped lines (may be 0): ");
            int skiplines = int.Parse(Console.ReadLine());
            Console.Write("Enter path to train set file: ");
            string trainSetPath = Console.ReadLine();
            Console.Write("Pictures as RGB (y/n): ");
            bool asRGB = Console.ReadLine() == "y";
            Console.Write("Enter headline (may be empty): ");
            string headline = Console.ReadLine();

            CreateTrainSetByFilesAndLabels(path, labelsPath, trainSetPath, skiplines, asRGB, headline);
        }

        static void TrainNeuralNetwork() {
            NeuralNetwork network;
            string[] classes;

            Console.Write("Create new network or load from file? (create/load): ");
            if (Console.ReadLine() == "load") {
                Console.Write("Enter path to network file: ");
                string path = Console.ReadLine();

                Console.Write("Enter classes (through space): ");
                classes = Console.ReadLine().Split(' ');

                network = new NeuralNetwork(path);
            }
            else {
                NeuroStructure structure;

                Console.Write("Enter number of inputs: ");
                structure.inputs = int.Parse(Console.ReadLine());
                Console.Write("Enter number of hiddens: ");
                string[] hiddens = Console.ReadLine().Split(' ');
                structure.hiddens = new int[hiddens.Length];

                for (int i = 0; i < hiddens.Length; i++)
                    structure.hiddens[i] = int.Parse(hiddens[i]);

                Console.Write("Enter classes (through space): ");
                classes = Console.ReadLine().Split(' ');
                structure.outputs = classes.Length;

                Console.Write("Enter activation function for hiddens layer(s) (0 - sigmoid, 1 - tanh, 2 - relu, 3 - linear): ");
                structure.hiddensFunction = (ActivationType)int.Parse(Console.ReadLine());
                Console.Write("Enter activation function for output layer (0 - sigmoid, 1 - tanh, 2 - relu, 3 - linear): ");
                structure.outputFunction = (ActivationType)int.Parse(Console.ReadLine());

                Console.Write("Enter name of network: ");
                structure.name = Console.ReadLine();

                network = new NeuralNetwork(structure);
            }

            TrainParameters parameters;

            parameters.dropOut = 0;
            parameters.printTime = true;

            Console.Write("Enter learning rate: ");
            parameters.learningRate = double.Parse(Console.ReadLine().Replace(".", ","));
            Console.Write("Enter accuracy: ");
            parameters.accuracy = double.Parse(Console.ReadLine().Replace(".", ","));
            Console.Write("Enter autosave period (0 - none): ");
            parameters.autoSavePeriod = int.Parse(Console.ReadLine());
            Console.Write("Enter max epochs: ");
            parameters.maxEpochs = int.Parse(Console.ReadLine());

            Console.Write("Enter path to train file: ");
            string trainPath = Console.ReadLine();
            Console.Write("Enter number of skipped lines: ");
            int skiplines = int.Parse(Console.ReadLine());

            TrainNeuralNetwork(network, parameters, trainPath, classes, skiplines);
        }

        static void PredictByNeuralNetwork() {
            Console.Write("Enter path to network file: ");
            string networkPath = Console.ReadLine();
            Console.Write("Enter path to test set file: ");
            string testPath = Console.ReadLine();
            Console.Write("Enter number of skipped lines: ");
            int skiplines = int.Parse(Console.ReadLine());
            Console.Write("Enter classes (through space): ");
            string[] classes = Console.ReadLine().Split(' ');
            Console.Write("Enter path to predict file: ");
            string predictPath = Console.ReadLine();
            Console.Write("Enter headline (may be empty): ");
            string headline = Console.ReadLine();

            PredictByNeuralNetwork(networkPath, testPath, skiplines, classes, predictPath, headline);

            Console.Write("Do you want to check accuracy? (y/n): ");
            bool check = Console.ReadLine() == "y";

            if (check) {
                Console.Write("Enter path to answers file: ");
                string answersPath = Console.ReadLine();
                CheckPredictions(predictPath, answersPath, false, skiplines);
            }
        }

        static void PredictByKNN() {
            Console.Write("Enter path to train set file: ");
            string trainPath = Console.ReadLine();
            Console.Write("Enter classes (through space): ");
            string[] classes = Console.ReadLine().Split(' ');
            Console.Write("Enter k for KNN: ");
            int k = int.Parse(Console.ReadLine());
            Console.Write("Enter path to test set file: ");
            string testPath = Console.ReadLine();
            Console.Write("Enter number of skipped lines: ");
            int skiplines = int.Parse(Console.ReadLine());
            Console.Write("Enter path to predict file: ");
            string predictPath = Console.ReadLine();
            Console.Write("Enter type of metric (0 - Euclid, 1 - Manhattan): ");
            KNearestNeighbors.MetricType type = (KNearestNeighbors.MetricType)int.Parse(Console.ReadLine());
            Console.Write("Enter headline (may be empty): ");
            string headline = Console.ReadLine();

            PredictByKNN(trainPath, classes, k, testPath, skiplines, predictPath, type, headline);

            Console.Write("Do you want to check accuracy? (y/n): ");
            bool check = Console.ReadLine() == "y";

            if (check) {
                Console.Write("Enter path to answers file: ");
                string answersPath = Console.ReadLine();
                CheckPredictions(predictPath, answersPath, false, skiplines);
            }
        }

        static void SplitToSets() {
            Console.Write("Enter path to total set file: ");
            string path = Console.ReadLine();
            Console.Write("Enter number of skipped lines: ");
            int skiplines = int.Parse(Console.ReadLine());
            Console.Write("Enter path to train set file: ");
            string trainPath = Console.ReadLine();
            Console.Write("Enter path to test set file: ");
            string testPath = Console.ReadLine();
            Console.Write("Enter path to answers file: ");
            string answersPath = Console.ReadLine();
            Console.Write("Enter train headline: ");
            string trainHeadline = Console.ReadLine();
            Console.Write("Enter test headline: ");
            string testHeadline = Console.ReadLine();
            Console.Write("Enter answers headline: ");
            string answersHeadline = Console.ReadLine();
            Console.Write("Enter fraction (test:train) for split (in 0.05..0.95): ");
            double fraction = double.Parse(Console.ReadLine().Replace(".", ","));

            SplitToSets(path, trainPath, testPath, answersPath, trainHeadline, testHeadline, answersHeadline, skiplines, fraction);
        }

        static void Main(string[] args) {
            int item;
            int exitItem = 10;

            do {
                Console.Clear();
                Console.WriteLine("What do you want to do?");
                Console.WriteLine("1. Check predictions");
                Console.WriteLine("2. Create answers from directories");
                Console.WriteLine("3. Create test set from directories");
                Console.WriteLine("4. Create train set from directories");
                Console.WriteLine("5. Create train set from directory and labels file");
                Console.WriteLine("6. Split set to train, test and answers");
                Console.WriteLine("7. Train neural network");
                Console.WriteLine("8. Predict by neural network (from file)");
                Console.WriteLine("9. Predict by KNN");
                Console.WriteLine("10. Exit");
                Console.Write(">");
                item = GetItem(exitItem);

                try {
                    switch (item) {
                        case 1:
                            CheckPredictions();
                            break;

                        case 2:
                            CreateAnswersByDirectories();
                            break;

                        case 3:
                            CreateTestSetByDirectories();
                            break;

                        case 4:
                            CreateTrainSetByDirectories();
                            break;

                        case 5:
                            CreateTrainSetByFilesAndLabels();
                            break;

                        case 6:
                            SplitToSets();
                            break;

                        case 7:
                            TrainNeuralNetwork();
                            break;

                        case 8:
                            PredictByNeuralNetwork();
                            break;

                        case 9:
                            PredictByKNN();
                            break;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("Exception: {0}. Try again", e.Message);
                }

                if (item != exitItem) {
                    Console.Write("Press key to continue...");
                    Console.ReadKey();
                }
            } while (item != exitItem);
        }
    }
}