﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Frohgame.Http;
using System.Xml.XPath;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

/*
 * 
 * Author(s): Purm & cannap
 * 
 */

namespace Frohgame.Core
{
	[Serializable()]
	public class FrohgameSession
	{
        #region Private Fields

		StringManager _stringManager;
		public StringManager StringManager {
			get {
				return this._stringManager;
			}
			set {
				_stringManager = value;
			}
		}		
        #endregion

        #region Properties
		
		/// <summary>
		/// Überprüft ob die session noch gültig ist
		/// </summary>
		/// <returns>
		/// <c>True</c> falls gültig, sonst <c>falsch</c>.
		/// </returns>
		/// <param name='refresh'>
		/// Wenn <c>true</c> wird die Overview Seite neu geladen, sonst wird aus dem cache geladen.
		/// </param>
		public bool IsLoggedIn(bool refresh) {
			try {
				if(refresh)
					NavigateToIndexPage(IndexPages.Overview);
				
				HtmlAgilityPack.HtmlNode node = AccountCache.LastIndexPageParser.DocumentNode.SelectSingleNode(this.StringManager.IsLoggedinXPath);
				if(node == null)
					return false;
					
				if(node.Attributes["content"] == null) {
					return false;	
				}
				
				return true;
			} catch(InvalidSessionException) {
				return false;	
			}
		}
		
		public Frohgame.Core.Mathemathics.Calculator Calculator = new Frohgame.Core.Mathemathics.Calculator();
		
		HttpHandler _httpHandler = new HttpHandler ("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:11.0) Gecko/20100101 Firefox/11.0");

		/// <summary>
		/// Http-Handler, zum abbonieren des Navigate Events
		/// </summary>
		public HttpHandler HttpHandler {
			get { return _httpHandler; }
			set { _httpHandler = value; }
		}

        /// <summary>
        /// Server on which the user plays
        /// </summary>
        public string Server {
            get;
            private set;
        }

		Logger _logger = new Logger ();
		/// <summary>
		/// Logging Handler, You can subscribe the logging events
		/// </summary>
		public Logger Logger {
			get { return _logger; }
			set { _logger = value; }
		}
		
		/// <summary>
		/// Anzahl der ungelesenen Nachrichten
		/// </summary>
		public int UnreadMessagesCount {
			get {
				if(AccountCache.LastIndexPageParser == null) {
					throw new Frohgame.Core.NoCacheDataException("AccountCache.LastIndexPageParser == null");	
				}
				
				HtmlAgilityPack.HtmlNode span = AccountCache.LastIndexPageParser.DocumentNode.SelectSingleNode(this.StringManager.UnreadMessageCountXPath);
				if(span == null) {
					throw new ParsingException("UnreadMessagesCount: Span-Knoten wurde nicht gefunden");
				}
				
				string innerText = span.InnerText;
				if(string.IsNullOrEmpty(innerText)) {
					throw new ParsingException("UnreadMessagesCount: Span-Knoten Inhalt ist leer");	
				}
				
				try {
					return Utils.StringReplaceToInt32WithoutPlusAndMinus(span.InnerText);
				} catch(FormatException) {
					//throw new ParsingException("UnreadMessagesCount: Span-Knoten Inhalt ist keine Zahl");
                    return 0;
				}
			}
		}
		
		Frohgame.FrohgameCache _accountCache = new Frohgame.FrohgameCache();
		public Frohgame.FrohgameCache AccountCache {
			get {
				return this._accountCache;
			}
			set {
				_accountCache = value;
			}
		}

		/// <summary>
		/// Liest die 'Ogame'-Version aus den Meta Daten aus
		/// </summary>
		public string Version {
			get {
				if(AccountCache.LastIndexPageParser == null) {
					throw new NoCacheDataException("AccountCache.lastIndexPageParser == null");	
				}
				
				HtmlAgilityPack.HtmlNode node = AccountCache.LastIndexPageParser.DocumentNode.SelectSingleNode(this.StringManager.VersionXPath);
				if(node == null) {
					throw new ParsingException("Version: Meta-Knoten konnte nicht gefunden werden");
				}
				
				HtmlAgilityPack.HtmlAttribute content = node.Attributes["content"];
				if(content == null) {
					throw new ParsingException("Version: Content-Attribut im Meta-Knoten konnte nicht gefunden werden");	
				}
				
				string version = content.Value;
				if(string.IsNullOrEmpty(version)) {
					throw new ParsingException("Version: Content-Attribut in Meta-Knoten ist leer");	
				}
				
				Logger.Log (LoggingCategories.Parse, "Version: " + version);
				return version;
			}
		}
		
		/// <summary>
		/// Token, welches beim Bau von Station-Gebäuden gebraucht wird
		/// </summary>
		public string StationToken {
			get {
				return GetToken(IndexPages.Station);	
			}
		}
		
		/// <summary>
		/// Token, welches beim Bau von Supply-Gebäuden gebraucht wird
		/// </summary>
		public string SupplyToken {
			get {
				return GetToken(IndexPages.Resources);	
			}
		}
		
		/// <summary>
		/// Token, welches beim Bau von Gebäuden gebraucht wird
		/// </summary>
		string GetToken(IndexPages page) {
			Planet currentPlanet = this.CurrentPlanet;
			if(currentPlanet == null) {
				throw new GeneralFrohgameException("currentPlanet == null");
			}
			
			if(currentPlanet.Cache.LastIndexPagesParsers[(int)page] == null) {
				throw new NoCacheDataException("CurrentPlanet.Cache.LastIndexPagesParsers[IndexPages." + page.ToString() + "] == null");
			}
			
			HtmlAgilityPack.HtmlNode inputNode = currentPlanet.Cache.LastIndexPagesParsers[(int)page].DocumentNode.SelectSingleNode(this.StringManager.TokenXPath);
			if(inputNode == null) {
				throw new ParsingException("StationToken: Input-Knoten konnte nicht gefunden werden");
			}
			
			HtmlAgilityPack.HtmlAttribute valueAttribute = inputNode.Attributes ["value"];
			if(valueAttribute == null) {
				throw new ParsingException("StationToken: value-Attribut vom Input-Knoten konnte nicht gefunden werden");	
			}
			
			string token = valueAttribute.Value;
			if(string.IsNullOrEmpty(token)) {
				throw new ParsingException("StationToken: value-Attribut vom Input-Knoten ist leer");
			}
			
			Logger.Log (LoggingCategories.Parse, "Token: " + token);
			return token;
		}
		
		/// <summary>
		/// Dunkle Materie
		/// </summary>
		public int DarkMatter {
			get {
				if(AccountCache.LastIndexPageParser == null) {
					throw new NoCacheDataException("AccountCache.LastIndexPageParser == null");	
				}
				
				HtmlAgilityPack.HtmlNode spanNode = AccountCache.LastIndexPageParser.DocumentNode.SelectSingleNode(this.StringManager.DarkMatterXPath);
				if(spanNode == null) {
					throw new ParsingException("DarkMatter: Span-Knoten konnte nicht gefunden werden");	
				}
				
				string tmp = spanNode.InnerText;
				if(string.IsNullOrEmpty(tmp)) {
					throw new ParsingException("DarkMatter: Span-Knoten ist leer");
				}
				
				int Result;
				try {
					Result = Utils.StringReplaceToInt32WithoutPlusAndMinus(tmp);
				} catch (FormatException) {
					throw new ParsingException("Darkmatter: Span-Knoten Inhalt ist keine Zahl");
				}
				Logger.Log (LoggingCategories.Parse, "DarkMatter: " + Result.ToString ());
				return Result;
			}
		}
		
		/// <summary>
		/// ID (aus den metadaten) vom aktuellen planeten
		/// </summary>
		public string CurrentPlanetId {
			get {
				if(AccountCache.LastIndexPageParser == null) {
					throw new NoCacheDataException("AccountCache.LastIndexPageParser == null");	
				}
				
				HtmlAgilityPack.HtmlNode metaNode = AccountCache.LastIndexPageParser.DocumentNode.SelectSingleNode(this.StringManager.CurrentPlanetIdXPath);
				if(metaNode == null) {
					throw new ParsingException("CurrentPlanetId: Meta-Knoten wurde nicht gefunden");	
				}
				
				HtmlAgilityPack.HtmlAttribute contentAttribute = metaNode.Attributes["content"];
				if(contentAttribute == null) {
					throw new ParsingException("CurrentPlanetId: content-Attribut vom Meta-Knoten konnte nicht gefuden werden");
				}
				
				string content = contentAttribute.Value;
				if(string.IsNullOrEmpty(content)) {
					throw new ParsingException("CurrentPlanetId: content-Attribut vom Meta-Knoten ist leer");	
				}
				
				return Utils.ReplaceEverythingsExceptNumbers(content);
			}
		}
		
		/// <summary>
		/// Der aktuell angewählte Planet
		/// </summary>
		public Planet CurrentPlanet {
		 	get {
				string currentPlanetId = CurrentPlanetId;
				if(!string.IsNullOrEmpty(currentPlanetId)) {
					foreach(Planet p in this.CachedPlanetList) {
						if(p.Id == currentPlanetId)	{
							return p;	
						}
					}
				}

				return null;
			}
		}
		
        #endregion

        #region Contructors

		/// <summary>
		/// Erstellt eine neue FrohgameSession
		/// </summary>
		/// <param name="userAgent">Useragent, der beim Browser-Simulator verwendet werden soll</param>
		public FrohgameSession (string userAgent)
            : this()
		{
			HttpHandler.UserAgent = userAgent;
		}

		/// <summary>
		/// Erstellt eine neue FrohgameSession
		/// </summary>
		public FrohgameSession()
		{
			this.StringManager = new StringManager();
		}
		
        #endregion

        #region Public Methods
		
		/// <summary>
		/// Navigiert zu einer Ogame-Standard Seite
		/// </summary>
		/// <param name="page">Ogame-Standard Seite</param>
		public HttpResult NavigateToIndexPage(IndexPages page)
		{
			Logger.Log (LoggingCategories.NavigationAction, "NavigateToIndexPage(" + this.StringManager.IndexPageNames [page] + ")");
            HttpResult tmp = HttpHandler.Get(this.StringManager.GetIndexPageUrl(page, this.Server));
			this.AccountCache.LastIndexPageResult = tmp;
			
			if(!this.IsLoggedIn(false)) {
				throw new Frohgame.Core.InvalidSessionException("Session abgelaufen oder invalidiert");
			}
			
			this.AccountCache.LastPageResult = tmp;
			this.AccountCache.LastIndexPagesResults[(int)page] = tmp;
			this.CurrentPlanet.Cache.LastPageResult = tmp;
			this.CurrentPlanet.Cache.LastIndexPagesResults[(int)page] =  tmp;
			this.CurrentPlanet.Cache.LastIndexPageResult =  tmp;
			
			return tmp;
		}

		/// <summary>
		/// Versucht sich einzuloggen
		/// </summary>
        public void Login(string name, string password, string server)
		{
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            if (string.IsNullOrEmpty(server))
                throw new ArgumentNullException("server");

            this.Server = server;

			Logger.Log (LoggingCategories.NavigationAction, "Login");
			//Zur Ogame Startseite navigieren
			Logger.Log (LoggingCategories.NavigationAction, "Navigate to Startpage");

			HttpHandler.Get(this.StringManager.StartUrl(server));

			//Logindaten senden^^
			Logger.Log (LoggingCategories.NavigationAction, "Sending Login Data");
			HttpResult tmp = HttpHandler.Post(this.StringManager.LoginUrl(server), this.StringManager.LoginParameter(server, name, password));
			
			this.AccountCache.LastIndexPageResult = tmp;
			
			if(!this.IsLoggedIn(false)) {
				throw new LoginFailedException ("Login failed (LogoutRegex not found)");
			}
			
			this.AccountCache.LastPageResult = tmp;
			this.AccountCache.LastIndexPagesResults[(int)IndexPages.Overview] = tmp;
			this.CurrentPlanet.Cache.LastPageResult = tmp;
			this.CurrentPlanet.Cache.LastIndexPagesResults[(int)IndexPages.Overview] =  tmp;
			this.CurrentPlanet.Cache.LastIndexPageResult =  tmp;
					
			Logger.Log (LoggingCategories.NavigationAction, "Login was successfull");
		}

		/// <summary>
		/// Versucht ein Gebäude auf dem aktuell-angewählten Planeten auszubauen
		/// </summary>
		/// <param name="building">gebäude id</param>
		public void UpgradeBuilding(SupplyBuildings building)
		{
			Logger.Log (LoggingCategories.NavigationAction, "UpgradeBuilding(" + building.ToString () + ")");
			if(this.AccountCache.LastIndexPageResult == null) {
				throw new NoCacheDataException("AccountCache.LastIndexPageResult == null");
			}
			//Falls Token nicht gefunden wird zur entsprechenden Seite navigieren
            if (this.AccountCache.LastIndexPageResult.ResponseUrl.ToString() != this.StringManager.GetIndexPageUrl(IndexPages.Resources, this.Server)) {
				Logger.Log (LoggingCategories.NavigationAction, "UpgradeBuilding: Wir sind noch nicht auf der Bau-Seite");
				NavigateToIndexPage (IndexPages.Resources);
			} else {
				Logger.Log (LoggingCategories.NavigationAction, "UpgradeBuilding: Wir sind bereits auf der Bau-Seite");
			}

			HttpResult tmp = NavigateBuildingAjax (1, building);

			HtmlAgilityPack.HtmlDocument tmpDoc = new HtmlAgilityPack.HtmlDocument ();
			tmpDoc.LoadHtml (tmp.ResponseContent);

			int neededMetal = GetMetalFromAjax (tmp.ResponseContent);
			int neededCrystal = GetCrystalFromAjax (tmp.ResponseContent);
			int neededDeuterium = GetDeuteriumFromAjax (tmp.ResponseContent);

			int currentLevel = GetBuildingLevelFromAjax (tmpDoc);

			//abgleichen mit aktuellen ressies
			if (this.CurrentPlanet.Metal < neededMetal) {
				throw new NotEnoughMetalException ("Nicht genug Metall zum bau von " + building.ToString ());
			} else if (this.CurrentPlanet.Crystal < neededCrystal) {
				Mathemathics.CalcMaxTimeForRes (this.CurrentPlanet.Metal, this.CurrentPlanet.Crystal, this.CurrentPlanet.Deuterium, neededMetal, neededCrystal, neededDeuterium, this.CurrentPlanet.MetalPerHour, this.CurrentPlanet.CrystalPerHour, this.CurrentPlanet.DeuteriumPerHour);
				throw new NotEnoughCrystalException ("Nicht genug Kristall zum bau von " + building.ToString ());
			} else if (this.CurrentPlanet.Deuterium < neededDeuterium) {
				throw new NotEnoughDeuteriumException ("Nicht genug Deuterium zum bau von " + building.ToString ());
			}
            HttpHandler.Post(this.StringManager.GetIndexPageUrl(IndexPages.Resources, this.Server), this.StringManager.GetUpgradeBuildingSubmitParameter(this.SupplyToken, building));
		}

		/// <summary>
		/// Versucht ein Gebäude auf dem aktuell-angewählten Planeten auszubauen
		/// </summary>
		/// <param name="building">gebäude id</param>
		public void UpgradeBuilding(StationBuildings building)
		{
			Logger.Log (LoggingCategories.NavigationAction, "UpgradeBuilding(" + building.ToString () + ")");
			if(this.AccountCache.LastIndexPageResult == null) {
				throw new NoCacheDataException("AccountCache.LastIndexPageResult == null");
			}
			//Falls Token nicht gefunden wird zur entsprechenden Seite navigieren
            if (this.AccountCache.LastIndexPageResult.ResponseUrl.ToString() != this.StringManager.GetIndexPageUrl(IndexPages.Station, this.Server)) {
				Logger.Log (LoggingCategories.NavigationAction, "UpgradeBuilding: Wir sind noch nicht auf der Bau-Seite");
				NavigateToIndexPage (IndexPages.Station);
			} else {
				Logger.Log (LoggingCategories.NavigationAction, "UpgradeBuilding: Wir sind bereits auf der Bau-Seite");
			}

			HttpResult tmp = NavigateBuildingAjax (1, building);

			HtmlAgilityPack.HtmlDocument tmpDoc = new HtmlAgilityPack.HtmlDocument ();
			tmpDoc.LoadHtml (tmp.ResponseContent);

			int neededMetal = GetMetalFromAjax (tmp.ResponseContent);
			int neededCrystal = GetCrystalFromAjax (tmp.ResponseContent);
			int neededDeuterium = GetDeuteriumFromAjax (tmp.ResponseContent);
			int currentLevel = GetBuildingLevelFromAjax (tmpDoc);

			//abgleichen mit aktuellen ressies
			if (this.CurrentPlanet.Metal < neededMetal) {
				throw new NotEnoughMetalException ("Nicht genug Metall zum bau von " + building.ToString ());
			} else if (this.CurrentPlanet.Crystal < neededCrystal) {
				Mathemathics.CalcMaxTimeForRes (this.CurrentPlanet.Metal, this.CurrentPlanet.Crystal, this.CurrentPlanet.Deuterium, neededMetal, neededCrystal, neededDeuterium, this.CurrentPlanet.MetalPerHour, this.CurrentPlanet.CrystalPerHour, this.CurrentPlanet.DeuteriumPerHour);
				throw new NotEnoughCrystalException ("Nicht genug Kristall zum bau von " + building.ToString ());
			} else if (this.CurrentPlanet.Deuterium < neededDeuterium) {
				throw new NotEnoughDeuteriumException ("Nicht genug Deuterium zum bau von " + building.ToString ());
			}
            HttpHandler.Post(this.StringManager.GetIndexPageUrl(IndexPages.Station, this.Server), this.StringManager.GetUpgradeBuildingSubmitParameter(this.StationToken, building));
		}

        #endregion

        #region Private Methods

		/// <summary>
		/// Öffnet den Ajax Javascript kram vom Gebäude-Baucenter
		/// </summary>
		/// <param name="ajaxIndex">immer 1 setzen vorerst (nochnicht herrausgefunden, was der parameter bringt)</param>
		/// <param name="ajaxParam">gebäude id</param>
		/// <returns>Http Ergebnis</returns>
		HttpResult NavigateBuildingAjax(int ajaxIndex, SupplyBuildings ajaxParam)
		{
			Logger.Log (LoggingCategories.NavigationAction, "NavigateBuildingAjax(" + ajaxIndex.ToString () + ", " + ajaxParam.ToString () + ")");
			return this._httpHandler.Post(this.StringManager.GetAjaxUrl(this.StringManager.IndexPageNames[IndexPages.Resources], ajaxIndex, this.Server), this.StringManager.GetAjaxParameter(ajaxParam));
		}

		/// <summary>
		/// Öffnet den Ajax Javascript kram vom Gebäude-Baucenter
		/// </summary>
		/// <param name="ajaxIndex">immer 1 setzen vorerst (nochnicht herrausgefunden, was der parameter bringt)</param>
		/// <param name="ajaxParam">gebäude id</param>
		/// <returns>Http Ergebnis</returns>
		HttpResult NavigateBuildingAjax(int ajaxIndex, StationBuildings ajaxParam)
		{
			Logger.Log (LoggingCategories.NavigationAction, "NavigateBuildingAjax(" + ajaxIndex.ToString () + ", " + ajaxParam.ToString () + ")");
            return this._httpHandler.Post(this.StringManager.GetAjaxUrl(this.StringManager.IndexPageNames[IndexPages.Station], ajaxIndex, this.Server), this.StringManager.GetAjaxParameter(ajaxParam));
		}

        /// <summary>
        /// Opens the Ajax of the message stuff
        /// </summary>
        /// <param name="ajaxIndex">everytime 1</param>
        /// <param name="page">page</param>
        /// <param name="category">category (every time 9)</param>
        /// <returns>the http result</returns>
        HttpResult NavigateMessageAjax(int ajaxIndex, int page, int categorie) {
            Logger.Log(LoggingCategories.NavigationAction, "NavigateMessageAjax(" + ajaxIndex + ", " + page + ", " + categorie + ")");
            return this._httpHandler.Post(this.StringManager.GetIndexPageUrl(IndexPages.Messages, this.Server), "displayCategory=" + categorie + "&displayPage=" + page + "&ajax=" + ajaxIndex);
        }

		/// <summary>
		/// Liest Level eines Gebäudes aus 
		/// </summary>
		/// <param name="ajaxHTML">Quelltext</param>
		/// <returns>Level</returns>
		private int GetBuildingLevelFromAjax(HtmlAgilityPack.HtmlDocument ajaxHTML)
		{
			int Result = Utils.StringReplaceToInt32WithoutPlusAndMinus(ajaxHTML.DocumentNode.SelectSingleNode(this.StringManager.BuildCurLevelXPath).InnerText);
			Logger.Log (LoggingCategories.Parse, "GetBuildingLevelFromAjax: " + Result.ToString ());
			return Result;
		}

		/// <summary>
		/// Liest Metal aus was gebraucht wird 
		/// </summary>
		/// <param name="ajaxHTML">Quelltext</param>
		/// <returns>Metal or 0 </returns>
		private int GetMetalFromAjax(string ajaxHTML)
		{
			string tmp = Utils.SimpleRegex(ajaxHTML, this.StringManager.NeededMetalRegex);
			int Result = 0;
			if (!string.IsNullOrEmpty (tmp)) {
				Result = Utils.StringReplaceToInt32WithoutPlusAndMinus (tmp);
			}

			Logger.Log (LoggingCategories.Parse, "GetMetalFromAjax: " + Result.ToString ());
			return Result;
		}

		/// <summary>
		/// Liest Kristall aus was gebraucht wird 
		/// </summary>
		/// <param name="ajaxHTML">Quelltext</param>
		/// <returns>Crystal or 0 </returns>
		private int GetCrystalFromAjax(string ajaxHTML)
		{
			string tmp = Utils.SimpleRegex(ajaxHTML, this.StringManager.NeededCrystalRegex);
			int Result = 0;
			if (!string.IsNullOrEmpty (tmp)) {
				Result = Utils.StringReplaceToInt32WithoutPlusAndMinus (tmp);
			}

			Logger.Log (LoggingCategories.Parse, "GetCrystalFromAjax: " + Result.ToString ());
			return Result;
		}

		/// <summary>
		/// Liest Deuterium aus was gebraucht wird 
		/// </summary>
		/// <param name="ajaxHTML">Quelltext</param>
		/// <returns>Deuterium or 0 </returns>
		private int GetDeuteriumFromAjax(string ajaxHTML)
		{
			string tmp = Utils.SimpleRegex(ajaxHTML, this.StringManager.NeededDeuteriumRegex);
			int Result = 0;
			if (!string.IsNullOrEmpty (tmp)) {
				Result = Utils.StringReplaceToInt32WithoutPlusAndMinus (tmp);
			}

			Logger.Log (LoggingCategories.Parse, "GetDeuteriumFromAjax: " + Result.ToString ());
			return Result;
		}
		
        #endregion

        #region Planet Switcher
		
		public List<Planet> _cachedPlanetList = null;
			
		public List<Planet> CachedPlanetList {
			get {
				if( _cachedPlanetList == null)
					_cachedPlanetList = PlanetList;
				
				return _cachedPlanetList;
			}
		}

        public List<FrohgameMessage> Messages {
            get {
                List<FrohgameMessage> ret = new List<FrohgameMessage>();

                HttpResult ajaxHtml = NavigateMessageAjax(1, 1, 9);

                return ret;
            }
        }
		
		/// <summary>
		/// Lese Planeten aus und füge Sie in planetList ein 
		/// </summary>
		public List<Planet> PlanetList {
			get {
				List<Planet> ret = new List<Planet> ();
				if(AccountCache.LastIndexPageParser == null) {
					throw new NoCacheDataException("AccountCache.LastIndexPageParser == null");	
				}
				
				HtmlAgilityPack.HtmlNodeCollection planetNodes = this.AccountCache.LastIndexPageParser.DocumentNode.SelectNodes (this.StringManager.PlanetListXPath);
				if(planetNodes == null) {
					throw new ParsingException("PlanetList: Div-Knoten nicht gefunden");
				}
				
				foreach (HtmlAgilityPack.HtmlNode planetNode in planetNodes) {
					ret.Add(new Planet (planetNode, this.StringManager, Logger));
				}

				return ret;
			}
		}

		/// <summary>
		/// Wechselt auf einen anderen Planet ist gültig für die ganze session
		/// </summary>
		/// <param name="planetListID">Die Nummer des Planeten</param>
		/// <returns>Planetname auf dem man sich nun Befindet</returns>
		public void ChangeToPlanet (IndexPages page, Planet planet)
		{
            HttpResult tmp = this.HttpHandler.Get(this.StringManager.GetIndexPageUrl(page, this.Server) + "&cp=" + planet.Id.ToString());
			this.AccountCache.LastIndexPageResult = tmp;
			
			if(!this.IsLoggedIn(false)) {
				throw new Frohgame.Core.InvalidSessionException("Session abgelaufen oder invalidiert");
			}
			
			this.AccountCache.LastPageResult = tmp;
			this.AccountCache.LastIndexPagesResults[(int)page] = tmp;
			this.CurrentPlanet.Cache.LastPageResult = tmp;
			this.CurrentPlanet.Cache.LastIndexPagesResults[(int)page] =  tmp;
			this.CurrentPlanet.Cache.LastIndexPageResult =  tmp;
		}

        #endregion
		
		#region Serialization
		
		/// <summary>
		/// Speichert die Session
		/// </summary>
		/// <param name='path'>
		/// Dateipfad
		/// </param>
		public void Serialize(string path, string iv, string key) {
            Utils.EncryptAndSerialize(path, this, iv, key);
		}
		
		/// <summary>
		/// Lädt eine Session aus einer Datei
		/// </summary>
        public static FrohgameSession Deserialize(string path, string iv, string key) {
            return (FrohgameSession)Utils.DecryptAndDeserialize(path, iv, key);
		}
		
		#endregion
	}
}