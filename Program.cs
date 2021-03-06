// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CoreFtp;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Samples.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ManageWebAppSourceControl
{
    public class Program
    {
        private const string Suffix = ".azurewebsites.net";

        /**
         * Azure App Service basic sample for managing web apps.
         * Note: you need to have the Git command line available on your PATH. The sample makes a direct call to 'git'.
         *  - Create 5 web apps under the same new app service plan:
         *    - Deploy to 1 using FTP
         *    - Deploy to 2 using local Git repository
         *    - Deploy to 3 using a publicly available Git repository
         *    - Deploy to 4 using a GitHub repository with continuous integration
         *    - Deploy to 5 using Web Deploy
         */
        public static void RunSample(IAzure azure)
        {
            string app1Name = SdkContext.RandomResourceName("webapp1-", 20);
            string app2Name = SdkContext.RandomResourceName("webapp2-", 20);
            string app3Name = SdkContext.RandomResourceName("webapp3-", 20);
            string app4Name = SdkContext.RandomResourceName("webapp4-", 20);
            string app5Name = SdkContext.RandomResourceName("webapp5-", 20);
            string app1Url = app1Name + Suffix;
            string app2Url = app2Name + Suffix;
            string app3Url = app3Name + Suffix;
            string app4Url = app4Name + Suffix;
            string app5Url = app5Name + Suffix;
            string rgName = SdkContext.RandomResourceName("rg1NEMV_", 24);

            try
            {
                //============================================================
                // Create a web app with a new app service plan

                Utilities.Log("Creating web app " + app1Name + " in resource group " + rgName + "...");

                var app1 = azure.WebApps
                        .Define(app1Name)
                        .WithRegion(Region.USWest)
                        .WithNewResourceGroup(rgName)
                        .WithNewWindowsPlan(PricingTier.StandardS1)
                        .WithJavaVersion(JavaVersion.V8Newest)
                        .WithWebContainer(WebContainer.Tomcat8_0Newest)
                        .Create();

                Utilities.Log("Created web app " + app1.Name);
                Utilities.Print(app1);

                //============================================================
                // Deploy to app 1 through FTP

                Utilities.Log("Deploying helloworld.War to " + app1Name + " through FTP...");

                Utilities.UploadFileToWebApp(
                    app1.GetPublishingProfile(), 
                    Path.Combine(Utilities.ProjectPath, "Asset", "helloworld.war"));

                Utilities.Log("Deployment helloworld.War to web app " + app1.Name + " completed");
                Utilities.Print(app1);

                // warm up
                Utilities.Log("Warming up " + app1Url + "/helloworld...");
                Utilities.CheckAddress("http://" + app1Url + "/helloworld");
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app1Url + "/helloworld...");
                Utilities.Log(Utilities.CheckAddress("http://" + app1Url + "/helloworld"));

                //============================================================
                // Create a second web app with local git source control

                Utilities.Log("Creating another web app " + app2Name + " in resource group " + rgName + "...");
                var plan = azure.AppServices.AppServicePlans.GetById(app1.AppServicePlanId);
                var app2 = azure.WebApps
                        .Define(app2Name)
                        .WithExistingWindowsPlan(plan)
                        .WithExistingResourceGroup(rgName)
                        .WithLocalGitSourceControl()
                        .WithJavaVersion(JavaVersion.V8Newest)
                        .WithWebContainer(WebContainer.Tomcat8_0Newest)
                        .Create();

                Utilities.Log("Created web app " + app2.Name);
                Utilities.Print(app2);

                //============================================================
                // Deploy to app 2 through local Git

                Utilities.Log("Deploying a local Tomcat source to " + app2Name + " through Git...");

                var profile = app2.GetPublishingProfile();
                Utilities.DeployByGit(profile, "azure-samples-appservice-helloworld");

                Utilities.Log("Deployment to web app " + app2.Name + " completed");
                Utilities.Print(app2);

                // warm up
                Utilities.Log("Warming up " + app2Url + "/helloworld...");
                Utilities.CheckAddress("http://" + app2Url + "/helloworld");
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app2Url + "/helloworld...");
                Utilities.Log(Utilities.CheckAddress("http://" + app2Url + "/helloworld"));

                //============================================================
                // Create a 3rd web app with a public GitHub repo in Azure-Samples

                Utilities.Log("Creating another web app " + app3Name + "...");
                var app3 = azure.WebApps
                        .Define(app3Name)
                        .WithExistingWindowsPlan(plan)
                        .WithNewResourceGroup(rgName)
                        .DefineSourceControl()
                            .WithPublicGitRepository("https://github.com/Azure-Samples/app-service-web-dotnet-get-started")
                            .WithBranch("master")
                            .Attach()
                        .Create();

                Utilities.Log("Created web app " + app3.Name);
                Utilities.Print(app3);

                // warm up
                Utilities.Log("Warming up " + app3Url + "...");
                Utilities.CheckAddress("http://" + app3Url);
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app3Url + "...");
                Utilities.Log(Utilities.CheckAddress("http://" + app3Url));

                //============================================================
                // Create a 4th web app with a personal GitHub repo and turn on continuous integration

                Utilities.Log("Creating another web app " + app4Name + "...");
                var app4 = azure.WebApps
                        .Define(app4Name)
                        .WithExistingWindowsPlan(plan)
                        .WithExistingResourceGroup(rgName)
                        // Uncomment the following lines to turn on 4th scenario
                        //.DefineSourceControl()
                        //    .WithContinuouslyIntegratedGitHubRepository("username", "reponame")
                        //    .WithBranch("master")
                        //    .WithGitHubAccessToken("YOUR GITHUB PERSONAL TOKEN")
                        //    .Attach()
                        .Create();

                Utilities.Log("Created web app " + app4.Name);
                Utilities.Print(app4);

                // warm up
                Utilities.Log("Warming up " + app4Url + "...");
                Utilities.CheckAddress("http://" + app4Url);
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app4Url + "...");
                Utilities.Log(Utilities.CheckAddress("http://" + app4Url));

                //============================================================
                // Create a 5th web app with web deploy

                Utilities.Log("Creating another web app " + app5Name + "...");

                IWebApp app5 = azure.WebApps.Define(app5Name)
                    .WithExistingWindowsPlan(plan)
                    .WithExistingResourceGroup(rgName)
                    .WithNetFrameworkVersion(NetFrameworkVersion.V4_6)
                    .Create();

                Utilities.Log("Created web app " + app5.Name);
                Utilities.Print(app5);

                //============================================================
                // Deploy to the 5th web app through web deploy

                Utilities.Log("Deploying a bakery website to " + app5Name + " through web deploy...");

                app5.Deploy()
                    .WithPackageUri("https://github.com/Azure/azure-libraries-for-net/raw/master/Tests/Fluent.Tests/Assets/bakery-webapp.zip")
                    .WithExistingDeploymentsDeleted(true)
                    .Execute();

                Utilities.Log("Deployment to web app " + app5Name + " completed.");
                Utilities.Print(app5);

                // warm up
                Utilities.Log("Warming up " + app5Url + "...");
                Utilities.CheckAddress("http://" + app5Url);
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app5Url + "...");
                Utilities.Log(Utilities.CheckAddress("http://" + app5Url));
            }
            catch (FileNotFoundException)
            {
                Utilities.Log("Cannot find 'git' command line. Make sure Git is installed and the directory of git.exe is included in your PATH environment variable.");
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rgName);
                    azure.ResourceGroups.DeleteByName(rgName);
                    Utilities.Log("Deleted Resource Group: " + rgName);
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }

        public static void Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var credentials = SdkContext.AzureCredentialsFactory.FromFile(Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));

                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                // Print selected subscription
                Utilities.Log("Selected subscription: " + azure.SubscriptionId);

                RunSample(azure);
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }
        }
    }
}