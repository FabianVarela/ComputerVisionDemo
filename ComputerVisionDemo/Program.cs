﻿using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Training;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ComputerVisionDemo
{
    class Program
    {
        private static string configTrainingKey = ConfigurationManager.AppSettings.Get("KeyTrainingCustomVision");
        private static string configPredictionKey = ConfigurationManager.AppSettings.Get("KeyPredictionCustomVision");

        private static List<MemoryStream> bikesImages;
        private static List<MemoryStream> rBikesImages;
        private static MemoryStream testImage;

        static void Main(string[] args)
        {
            string trainingKey = GetTrainingKey(configTrainingKey, args);
            TrainingApi trainingApi = new TrainingApi { ApiKey = trainingKey };

            Console.WriteLine("Creating new project:");

            var project = trainingApi.CreateProject("Bike Type");
            var MbikesTag = trainingApi.CreateTag(project.Id, "Mountain");
            var RbikesTag = trainingApi.CreateTag(project.Id, "Racing");

            Console.WriteLine("\tUploading images");
            LoadImages();

            foreach (var image in bikesImages)
                trainingApi.CreateImagesFromData(project.Id, image, new List<string>() { MbikesTag.Id.ToString() });

            foreach (var image in rBikesImages)
                trainingApi.CreateImagesFromData(project.Id, image, new List<string>() { RbikesTag.Id.ToString() });

            trainingApi.CreateImagesFromData(project.Id, testImage, new List<string>() { MbikesTag.Id.ToString() });

            Console.WriteLine("\tTraining");
            var iteration = trainingApi.TrainProject(project.Id);

            while (iteration.Status.Equals("Training"))
            {
                Thread.Sleep(1000);
                iteration = trainingApi.GetIteration(project.Id, iteration.Id);
            }

            iteration.IsDefault = true;
            trainingApi.UpdateIteration(project.Id, iteration.Id, iteration);

            Console.WriteLine("Done!\n");

            var predictionKey = GetPredictionKey(configPredictionKey, args);
            PredictionEndpoint endpoint = new PredictionEndpoint { ApiKey = predictionKey };

            Console.WriteLine("Making a prediction:");
            var result = endpoint.PredictImage(project.Id, testImage);

            foreach (var c in result.Predictions)
                Console.WriteLine($"\t{c.Tag}: {c.Probability:P1}");

            Console.ReadKey();
        }

        private static string GetTrainingKey(string trainingKey, string[] args)
        {
            if (string.IsNullOrWhiteSpace(trainingKey) || trainingKey.Equals(configTrainingKey))
            {
                if (args.Length >= 1)
                    trainingKey = args[0];

                while (string.IsNullOrWhiteSpace(trainingKey) || trainingKey.Length != 32)
                {
                    Console.Write("Enter your training key: ");
                    trainingKey = Console.ReadLine();
                }

                Console.WriteLine();
            }

            return trainingKey;
        }

        private static string GetPredictionKey(string predictionKey, string[] args)
        {
            if (string.IsNullOrWhiteSpace(predictionKey) || predictionKey.Equals(configPredictionKey))
            {
                if (args.Length >= 2)
                    predictionKey = args[1];

                while (string.IsNullOrWhiteSpace(predictionKey) || predictionKey.Length != 32)
                {
                    Console.Write("Enter your prediction key: ");
                    predictionKey = Console.ReadLine();
                }

                Console.WriteLine();
            }

            return predictionKey;
        }

        private static void LoadImages()
        {
            bikesImages = Directory.GetFiles(@"Images\Mountain").Select(path => new MemoryStream(File.ReadAllBytes(path))).ToList();
            rBikesImages = Directory.GetFiles(@"Images\Racing").Select(path => new MemoryStream(File.ReadAllBytes(path))).ToList();
            testImage = new MemoryStream(File.ReadAllBytes(@"Images\test\bike1.jpg"));
        }
    }
}
