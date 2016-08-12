﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using NHttp;

namespace OfflineServer.Servers.Http
{
    public class HttpServer
    {
        // for easy development and debug, will be removed later when everything is coded
        private List<String> supportedMethods = new List<string>() { "secureLoginPersona", "secureLogout", "getChatInfo", "carslots", "getPersonaInfo", "getPersonaBaseFromList", "getPermanentSession" };
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public NHttp.HttpServer nServer = new NHttp.HttpServer();
        public Int32 port;
        public String postData;
        public HttpServer()
        {
            nServer.RequestReceived += nServer_RequestReceived;
            nServer.EndPoint.Port = 1337; // debug
            nServer.Start();
            port = nServer.EndPoint.Port;
            log.Info(String.Format("Successfully setup HttpServer on port {0}.", port));
        }
        
        private void nServer_RequestReceived(object sender, HttpRequestEventArgs e)
        {
            e.Response.Headers.Add("Connection", "close");
            e.Response.Headers.Add("Content-Encoding", "gzip");
            e.Response.Headers.Add("Content-Type", "application/xml;charset=utf-8");
            e.Response.Headers.Add("Status-Code", "200");
            
            log.Info(String.Format("Received Http-{0} request from {1}.", e.Request.HttpMethod, e.Request.RawUrl));

            Byte[] baResponseArray = null;
            String[] splittedPath = e.Request.Path.Split('/');
            String ioPath = e.Request.Path.Remove(0, 1) + ".xml";

            if (splittedPath.Length > 5)
            {
                String targetClassString = changeCaseFirst(splittedPath[4], true);
                double dummy;
                Boolean isNumber = double.TryParse(splittedPath[5], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out dummy);
                String targetMethodString = changeCaseFirst(isNumber ? splittedPath[6] : splittedPath[5], false);
                if (!supportedMethods.Contains(targetMethodString))
                {
                    log.Warn(String.Format("Method for {0} wasn't found, using fallback XML method.", targetMethodString));
                    if (File.Exists(ioPath))
                    {
                        log.Info(String.Format("Reading XML file {0}.", ioPath));
                        baResponseArray = getResponseData(File.ReadAllText(ioPath, Encoding.UTF8));
                    }
                    else
                    {
                        log.Warn(String.Format("File {0} wasn't found, sending only 200OK.", ioPath));
                    }
                }
                else
                {
                    Type targetClass = Type.GetType("OfflineServer.Servers.Http.Classes." + targetClassString);
                    MethodInfo targetMethod = targetClass.GetMethod(targetMethodString);
                    if (e.Request.HttpMethod == "POST")
                        using (StreamReader reader = new StreamReader(e.Request.InputStream))
                            postData = reader.ReadToEnd();
                    log.Info(String.Format("Processing OfflineServer.HttpServer.Classes.{0}.{1}().", targetClassString, targetMethodString));
                    baResponseArray = getResponseData((string)targetMethod.Invoke(null, null));
                }
            }
            else
            {
                if (File.Exists(ioPath))
                {
                    log.Info(String.Format("Reading XML file {0}.", ioPath));
                    baResponseArray = getResponseData(File.ReadAllText(ioPath, Encoding.UTF8));
                } else
                {
                    log.Warn(String.Format("File {0} wasn't found, sending only 200OK.", ioPath));
                }
            }

            if (baResponseArray == null) baResponseArray = getResponseData(" ");
            e.Response.OutputStream.Write(baResponseArray, 0, baResponseArray.Length);
            e.Response.OutputStream.Flush();

            // e.Request.RequestType gives the method used, GET - POST - PUSH etc.
            // e.Request.Url gives the full Uri including EVERYTHING
            // e.Request.RawUrl gives the Path following the IP. EX: if 127.0.0.1:4444/test/path.xml?test=true then /test/path.xml?test=true
            // e.Request.Path gives only the Path, not adding the params at the end. EX: if 127.0.0.1:4444/test/path.xml?test=true then /test/path.xml
            // e.Request.Params gives only the Params, not adding anything else.
        }

        private Byte[] getResponseData(String responseText)
        {
            Byte[] baAnswerData = Encoding.UTF8.GetBytes(responseText);

            using (MemoryStream msResponse = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(msResponse, CompressionMode.Compress, true))
                    gzip.Write(baAnswerData, 0, baAnswerData.Length);

                return msResponse.ToArray();
            }
        }

        private string changeCaseFirst(String input, Boolean upperCase)
        {
            char[] cArray = input.ToCharArray();
            cArray[0] = upperCase ? char.ToUpper(cArray[0]): char.ToLower(cArray[0]);
            return new string(cArray);
        }
    }
}