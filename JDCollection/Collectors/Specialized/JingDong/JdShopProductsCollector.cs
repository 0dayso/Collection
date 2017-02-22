namespace X.GlodEyes.Collectors.Specialized.JingDong
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml.XPath;

    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    using X.CommLib.Net.Miscellaneous;
    using X.CommLib.Office;

    using NUnit.Framework;

  
    /// <summary>
    ///     ����������Ʒ����
    /// </summary>
    internal class JdShopProductsCollector : WebRequestCollector<IResut, NormalParameter>
    {
     
        /// <summary>
        ///     û�ж����һ������ֵ
        /// </summary>
        private const string NotInitId = "NotInit";

        /// <summary>
        ///     ���̵�ַ
        /// </summary>
        /// <value>
        ///     The shop URL.
        /// </value>
        public string ShopUrl { get; private set; }

        /// <summary>
        ///     Ϊ�˲���
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            var result = base.MoveNext();

#if DEBUG
            if (this.Current?.Length == 0)
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var store = Path.Combine(desktop, @"�����ļ�");
                Directory.CreateDirectory(store);

                var matchResults = Regex.Match(this.ShopUrl, @"(?<=index-)\d+(?=\.html)");
                var shopId = matchResults.Success ? matchResults.Value : $"{Guid.NewGuid()}";

                var savePath = Path.Combine(store, $"{shopId} - {this.CurrentPage}.txt");
                File.WriteAllText(savePath, this.HtmlSource);
            }

#endif

            return result;
        }

 
        /// <summary>
        ///     Tests this instance.
        /// </summary>
        public static void Test()
        {
            var shopIds = File.ReadAllLines(@"C:\Users\Administrator\Desktop\test.txt");

            /*var shopIds = File.ReadAllLines(@"C:\Users\sinoX\Desktop\errorList.txt");*/
            // ȥ���ַ���ǰ���"
            shopIds = Array.ConvertAll(shopIds, shopId => shopId.Trim('"'));

            foreach (var shopId in shopIds)
            {
                Console.WriteLine();
                Console.WriteLine($"shop id: {shopId}");
                try
                {
                    var parameter = new NormalParameter { Keyword = shopId };
                    TestHelp<JdShopProductsCollector>(parameter, 1);
                }
                catch (NotSupportedException exception)
                {
                    Console.WriteLine($"error: {exception.Message}");
                }
            }
        }

        /// <summary>
        ///     ������һ�ҵ���
        /// </summary>
        internal static void TestSimplyRun()
        {
            var parameter = new NormalParameter { Keyword = @"1000004123" };
            TestHelp<JdShopProductsCollector>(parameter, 3);
        }

        /// <summary>
        ///     ���ص�ǰ��ҳ�������
        /// </summary>
        /// <param name="nextUrl">The next URL.</param>
        /// <param name="postData">The post data.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="currentUrl">The current URL.</param>
        /// <returns></returns>
        protected override string GetMainWebContent(
            string nextUrl,
            byte[] postData,
            ref string cookies,
            string currentUrl)
        {
            var webContent = this.GetWebContent(nextUrl, postData, ref cookies, currentUrl, true);

            return this.ParseModuleText(webContent);
        }

        /// <summary>
        ///     Ϊ��һҳ����ʼ��׼��
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <returns></returns>
        protected override string InitFirstUrl(NormalParameter param)
        {
            var shopId = param.Keyword;

            if (StringExtension.IsNullOrWhiteSpace(shopId))
            {
                throw new ArgumentException(@"û��ָ�����̱��");
            }

            this.ShopUrl = $"http://mall.jd.com/index-{shopId}.html";

            return NotInitId;
        }

        /// <summary>
        ///     �ƶ���һҳ����һҳҪ�ر���
        /// </summary>
        /// <returns>
        ///     System.String.
        /// </returns>
        protected override string MoveToNextPage()
        {
            return this.NextUrl == NotInitId ? this.MoveToFirstPage() : base.MoveToNextPage();
        }

        /// <summary>
        ///     ��������ҳ��
        /// </summary>
        /// <returns></returns>
        protected override int ParseCountPage()
        {
            return this.ParseAmountPage(this.HtmlSource);
        }

        /// <summary>
        ///     ��������ǰֵ
        /// </summary>
        /// <returns></returns>
        protected override IResut[] ParseCurrentItems()
        {
            return this.ParseCurrentItems(this.HtmlSource);
        }

        /// <summary>
        ///     ��������ǰҳ��
        /// </summary>
        /// <returns></returns>
        protected override int ParseCurrentPage()
        {
            var currentUrl = this.CurrentUrl;
            if (string.IsNullOrEmpty(currentUrl))
            {
                return -1;
            }

            var collection = Url.ParseQueryString(currentUrl);

            int page;
            return int.TryParse(collection[@"pageNo"], out page) ? page : -1;
        }



        /// <summary>
        ///     ��������һҳ�ĵ�ַ��Ĭ�ϵ�û����һҳ��ʱ��ö��ֹͣ
        /// </summary>
        /// <returns></returns>
        protected override string ParseNextUrl()
        {
            if (this.IsEmptyPage())
            {
                return null;
            }

            var currentUrl = this.CurrentUrl;
            if (StringExtension.IsNullOrWhiteSpace(currentUrl))
            {
                return null;
            }

            var nextpageUrl = HtmlDocumentHelper.GetValueByXPath(this.HtmlSource, @"//a[text()='��һҳ']/@href");
            if (StringExtension.IsNullOrWhiteSpace(nextpageUrl))
            {
                return null;
            }

            string baseUrl;
            NameValueCollection collection;
            Url.ParseUrl(currentUrl, out baseUrl, out collection);

            var pageNo = int.Parse(collection[@"pageNo"]);
            collection[@"pageNo"] = $"{pageNo + 1}";

            return Url.CombinUrl(baseUrl, collection);
        }

        /// <summary>
        ///     ������������
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private static string ParseComments(XPathNavigator item)
        {
            /*var matchResults = Regex.Match(item.Value, @"(?<=���� *)\d+(?= *������)");*/
            var matchResults = Regex.Match(item.Value, @"\d+(?= *������)");
            if (matchResults.Success)
            {
                return matchResults.Value;
            }

            var commentsNode = HtmlDocumentHelper.GetNodeValue(item, ".//span[@class='evaluate']");
            matchResults = Regex.Match(commentsNode, @"(?<=\()\d+(?=\))");
            if (matchResults.Success)
            {
                return matchResults.Value;
            }

            return "-1";
        }

        /// <summary>
        ///     ��������  ajax url
        /// </summary>
        /// <param name="webContent">Content of the web.</param>
        /// <param name="renderStructure">The render structure.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">$�޷���ҳ���н�������������</exception>
        private string BuildAjaxSearchUrl(string webContent, IDictionary<string, string> renderStructure)
        {
            /*webContent = File.ReadAllText(@"C:\Users\sinoX\Desktop\��������ҳ.html");*/
            var matchResults = Regex.Match(webContent, "(?<=var params = ){[^}]+}");
            if (!matchResults.Success)
            {
                throw new NotSupportedException("�޷���ҳ���н�������������");
            }

            // {"appId":"435517","orderBy":"5","direction":"0","categoryId":"0","pageSize":"24","venderId":"1000004373","isGlobalSearch":"0","maxPrice":"0","pagePrototypeId":"17","pageNo":"1","shopId":"1000004373","minPrice":"0"}
            var jObject = JObject.Parse(matchResults.Value);
            var navigator = HtmlDocumentHelper.CreateNavigator(webContent);

            
            System.Func<string, string> readJsonFunc = key => JsonHelper.TryReadJobjectValue(jObject, key, (string)null);
            System.Func<string, string> readHtmlFunc = key =>
                {
                    string value;
                    renderStructure.TryGetValue(key, out value);
                    return value;
                };
            System.Func<string, string> readInputFunc =
                key => HtmlDocumentHelper.GetNodeValue(navigator, $@"//input[@id='{key}']/@value");

            var collection = Url.CreateQueryCollection();

            collection[@"appId"] = readJsonFunc("appId");
            collection[@"orderBy"] = "5";
            collection[@"pageNo"] = "1";
            collection[@"direction"] = "1";
            collection[@"categoryId"] = readJsonFunc(@"categoryId");
            collection[@"pageSize"] = @"24";
            collection[@"pagePrototypeId"] = readJsonFunc(@"pagePrototypeId");
            collection[@"pageInstanceId"] = readHtmlFunc(@"m_render_pageInstance_id");
            collection[@"moduleInstanceId"] = readHtmlFunc(@"m_render_instance_id");
            collection[@"prototypeId"] = readHtmlFunc(@"m_render_prototype_id");
            collection[@"templateId"] = readHtmlFunc(@"m_render_template_id");
            collection[@"layoutInstanceId"] = readHtmlFunc(@"m_render_layout_instance_id");
            collection[@"origin"] = readHtmlFunc(@"m_render_origin");
            collection[@"shopId"] = readInputFunc(@"shop_id");
            collection[@"venderId"] = readInputFunc(@"vender_id");

            /*collection[@"callback"] = @"jshop_module_render_callback";  // �������ֱ�ӷ���һ�� json �ṹ */
            collection[@"_"] = $"{JsCodeHelper.GetDateTime()}";

            var baseUrl = renderStructure[@"m_render_is_search"] == "true"
                              ? @"http://module-jshop.jd.com/module/getModuleHtml.html"
                              : @"http://mall.jd.com/view/getModuleHtml.html";

            return Url.CombinUrl(baseUrl, collection);
        }


        /// <summary>
        ///     ��������ҳ�������
        /// </summary>
        /// <param name="shopUrl">The shop URL.</param>
        /// <returns></returns>
        private string GetSearchPageContent(string shopUrl)
        {
            var webContent = this.GetWebContent(shopUrl);
            // ȡ��<input type="hidden" value="504028" id="pageInstance_appId"/>�е�value
            var pageAppId = HtmlDocumentHelper.GetNodeValue(webContent, @"//input[@id='pageInstance_appId']/@value");

            /*
             // view_search-����ҳ����-0-��������-������-ÿҳ����-ҳ��.html
                �������ͣ� 5 ���� 4 �۸� 3 �ղ�  2 ʱ�� 
                ÿҳ������ ��� 24 
                ������ 1 �Ӵ�ʱС  0 ��С����
                ҳ�룺 �� 1 ��ʼ 
                ����pageInstance_appId�ҵ�value��ֵ
                http://mall.jd.com//view_search-337310-0-5-0-24-5.html
             */
            var searchUrl = $"http://mall.jd.com/view_search-{pageAppId}-0-5-0-24-1.html";

#if DEBUG
            Console.WriteLine($"������ַ��{searchUrl}");
#endif

            return this.GetWebContent(searchUrl);
        }

        /// <summary>
        ///     �Ƽ��ǲ�����Ʒҳ��
        /// </summary>
        /// <param name="webContent">Content of the web.</param>
        /// <returns></returns>
        private bool GuessIsSearchWebContent(string webContent)
        {
            if (string.IsNullOrEmpty(webContent))
            {
                return false;
            }

            var navigator = HtmlDocumentHelper.CreateNavigator(webContent);
            var iterator = navigator.Select(@"//a");

            var itemCount = 0;

            foreach (XPathNavigator item in iterator)
            {
                var href = item.GetAttribute(@"href", string.Empty);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (href == null)
                {
                    continue;
                }

                if (Regex.IsMatch(href, @"^//item\.jd\.com/\d+\.html", RegexOptions.IgnoreCase))
                {
                    itemCount++;
                }

                if (itemCount >= 8)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Determines whether [is empty page].
        /// </summary>
        /// <returns></returns>
        private bool IsEmptyPage()
        {
            return StringExtension.IsNullOrWhiteSpace(this.HtmlSource);
        }

        /// <summary>
        ///     ���Ը�ҳ���ǲ����������ҳ
        /// </summary>
        /// <param name="searchContent">Content of the search.</param>
        /// <returns></returns>
        private bool IsSearchWebContent(string searchContent)
        {
            var page = this.ParseAmountPage(searchContent);
            return page != -1;
        }

        /// <summary>
        ///     �������ֲ���, �ǻ�����һ��������֪������һ��������ȫ������
        /// </summary>
        /// <param name="webContent">Content of the web.</param>
        /// <returns></returns>
        private IDictionary<string, string>[] LoadRenderStructures(string webContent)
        {
            var results = new List<IDictionary<string, string>>();

            var matchResultses = Regex.Matches(webContent, "<[^>]+\"m_render_structure loading\"[^>]+>");
            foreach (Match matchResultse in matchResultses)
            {
                IDictionary<string, string> result = new Dictionary<string, string>();
                var matchResults = Regex.Match(matchResultse.Value, @"(?<key>\w+)=""(?<val>[^""]+)""");
                 while (matchResults.Success)
                {
                    result[matchResults.Groups["key"].Value] = matchResults.Groups["val"].Value;

                    matchResults = matchResults.NextMatch();
                 }

                results.Add(result);
            }

            return results.ToArray();
        }

        /// <summary>
        ///     �ƶ�����һҳ
        /// </summary>
        /// <returns></returns>
        private string MoveToFirstPage()
        {
            var webContent = this.GetSearchPageContent(this.ShopUrl);

            var renderStructures = this.LoadRenderStructures(webContent);
            var contents = new List<KeyValuePair<string, string>>();

            foreach (var renderStructure in renderStructures)
            {
                var ajaxSearchUrl = this.BuildAjaxSearchUrl(webContent, renderStructure);
                var searchContent = this.GetWebContent(ajaxSearchUrl);
                var content = this.ParseModuleText(searchContent);

                contents.Add(new KeyValuePair<string, string>(ajaxSearchUrl, searchContent));

                if (!this.IsSearchWebContent(content))
                {
                    continue;
                }

                this.HtmlSource = content;
                this.CurrentUrl = ajaxSearchUrl;

                return content;
            }

            foreach (var contentItem in contents)
            {
                var content = this.ParseModuleText(contentItem.Value);
                if (!this.GuessIsSearchWebContent(content))
                {
                    continue;
                }

                this.HtmlSource = content;
                this.CurrentUrl = contentItem.Key;

                return content;
            }

            throw new NotSupportedException("û�н�������Ч������ҳ��");
        }

        /// <summary>
        ///     ������ǰ��ҳ��
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        private int ParseAmountPage(string content)
        {
            if (content == null)
            {
                return -1;
            }

            var navigator = HtmlDocumentHelper.CreateNavigator(content);
            var node = navigator.SelectSingleNode(@"//div[@class='jPage']/em");
            if (node == null)
            {
                return -1;
            }

            /*
                 <div class="jPage">
    	        <em>��104����¼</em>
                <span>��һҳ</span>
                <a class="current">1</a>
                <a href="//mall.jd.com/view_search-504028-1000007084-1000007084-0-5-0-0-1-2-24.html?isGlobalSearch=0">2</a>
                <a href="//mall.jd.com/view_search-504028-1000007084-1000007084-0-5-0-0-1-3-24.html?isGlobalSearch=0">3</a>
                <span>...</span>
                <a href="//mall.jd.com/view_search-504028-1000007084-1000007084-0-5-0-0-1-5-24.html?isGlobalSearch=0">5</a>
                <a href="//mall.jd.com/view_search-504028-1000007084-1000007084-0-5-0-0-1-2-24.html?isGlobalSearch=0">��һҳ</a>
		        </div>
             */
            var matchResults = Regex.Match(node.Value, @"(?<=��)\d+(?=����¼)");
            return matchResults.Success ? int.Parse(matchResults.Value) : -1;
        }

        /// <summary>
        ///     ��������Ʒ
        /// </summary>
        /// <param name="htmlSource">The HTML source.</param>
        /// <param name="listOnly">���������б��������۸����Ҫ�ٴη������������.</param>
        /// <returns></returns>
        private IResut[] ParseCurrentItems(string htmlSource, bool listOnly = false)
        {
/*
#if DEBUG
            htmlSource = "";
            var htmlSources = File.ReadAllLines(@"C:\Users\Administrator\Desktop\htmlSource.txt",System.Text.Encoding.UTF8);
            for (int i=0;i< htmlSources.Length;i++)
            {
                htmlSource += htmlSources[i];
            }

#endif
*/
            const string SkuIdKey = "ProductSku";
            var resultList = new List<IResut>();

            var navigator = HtmlDocumentHelper.CreateNavigator(htmlSource);
            var iterator = navigator.Select(@"//ul/li");

            foreach (XPathNavigator item in iterator)
            {
                var title = HtmlDocumentHelper.GetNodeValue(item, ".//div[@class='jDesc']/a/text()");
                var href = HtmlDocumentHelper.GetNodeValue(item, ".//div[@class='jDesc']//@href");
                if (string.IsNullOrEmpty(title))
                {
                    title = HtmlDocumentHelper.GetNodeValue(item, ".//div[@class='jTitle']/a/text()");
                    href = HtmlDocumentHelper.GetNodeValue(item, ".//div[@class='jTitle']//@href");
                }

                var imgSrc = HtmlDocumentHelper.GetNodeValue(item, ".//div[@class='jPic']//@original");
                var skuMatchResults = Regex.Match(href, @"(?<=/)\d+(?=\.html)");
                var sku = skuMatchResults.Success ? skuMatchResults.Value : string.Empty;

                if (string.IsNullOrEmpty(sku))
                {
                    continue;
                }

                // �������� 
                var comments = ParseComments(item);

                IResut resut = new Resut();

                resut[SkuIdKey] = sku;
                resut["ShopId"] = ShopUrl;
                resut["ProductName"] = title;
                resut["ProductUrl"] = href;
                resut["ProductImage"] = imgSrc;
                resut["ProductComments"] = comments;
                resultList.Add(resut);
            }

            if (!listOnly)
            {
                this.UpdateResultsPrices(resultList, SkuIdKey);
            }

            return resultList.ToArray();
        }

        /// <summary>
        ///     ���� json �е�����
        /// </summary>
        /// <param name="webContent">Content of the web.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">$û�дӽ���н�������Ʒ����:{this.Current}</exception>
        private string ParseModuleText(string webContent)
        {
            var matchResults = Regex.Match(webContent, @"(?<=^\w+\(){.*}(?=\))");
            webContent = matchResults.Success ? matchResults.Value : webContent;

            var jObject = JObject.Parse(webContent);
            var jToken = jObject[@"moduleText"];
            if (jToken == null)
            {
                throw new NotSupportedException($"û�дӽ���н�������Ʒ����:{this.Current}");
            }

            return jToken.Value<string>();
        }

        /// <summary>
        ///     ���¼۸��б�
        /// </summary>
        /// <param name="resultList">The result list.</param>
        /// <param name="skuIdKey">The sku identifier key.</param>
        private void UpdateResultsPrices(List<IResut> resultList, string skuIdKey)
        {
            if (resultList.Count == 0)
            {
                return;
            }

            IDictionary<string, IResut> resultDictionary = new Dictionary<string, IResut>();
            resultList.ForEach(result => resultDictionary[$"J_{result[skuIdKey]}"] = result);

            /*foreach(var result in resultList)
            {
                var keyname = result[skuIdKey];
                resultDictionary[$"J_{keyname}"] = result
            }*/


            var skuids = new string[resultDictionary.Count];
            resultDictionary.Keys.CopyTo(skuids, 0);

            var collection = Url.CreateQueryCollection();
            collection[@"skuids"] = string.Join(",", skuids);
            collection[@"_"] = $"{JsCodeHelper.GetDateTime()}";

            // http://p.3.cn/prices/mgets?skuids=J_1077038109,J_10134047427,J_10344905938,J_10377204212&type=2&callback=callBackPriceService&_=1466737672671
            const string BaseUrl = @"http://p.3.cn/prices/mgets";
            var url = Url.CombinUrl(BaseUrl, collection);

            var webContent = this.GetWebContent(url);

            var jArray = JArray.Parse(webContent);
            foreach (var jToken in jArray)
            {
                var skuid = JsonHelper.ReadJobjectValue<string>(jToken, @"id");
                var price = JsonHelper.ReadJobjectValue<string>(jToken, @"p");
                var mprice = JsonHelper.ReadJobjectValue<string>(jToken, @"m");

                IResut result;
                if (!resultDictionary.TryGetValue(skuid, out result)) continue;

                result[@"ProductPrice"] = price;
                result[@"ProductMPrice"] = mprice;
            }
        }



    }


   
}