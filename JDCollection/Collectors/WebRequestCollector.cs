namespace X.GlodEyes.Collectors
{
    using System;
    using System.Collections.Generic;

    using X.CommLib.Net.WebRequestHelper;
    using X.CommLib.Office;

    /// <summary>
    ///     ���� webRequest �ķ��͹���
    /// </summary>
    public abstract class WebRequestCollector<TResut, TParam> : NormalCollector, 
                                                                IEnumerator<TResut[]>, 
                                                                IEnumerable<TResut[]>
        where TResut : IResut where TParam : IParameter
    {
        /// <summary>
        ///     ���������е� Cookies ֵ
        /// </summary>
        /// <value>
        ///     The cookies.
        /// </value>
        public string Cookies { get; set; }

        /// <summary>
        ///     ���ص�ǰ�Ĳɼ�ֵ
        /// </summary>
        /// <value>
        ///     The current.
        /// </value>
        public new TResut[] Current { get; protected set; }

        /// <summary>
        ///     �Ƿ��и��������
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance has more; otherwise, <c>false</c>.
        /// </value>
        public bool HasMore { get; protected set; }

        /// <summary>
        ///     ��ǰҳ���Դ��
        /// </summary>
        /// <value>
        ///     The HTML source.
        /// </value>
        public string HtmlSource { get; set; }

        /// <summary>
        ///     The inner parameter
        /// </summary>
        public TParam InnerParam { get; private set; }

        /// <summary>
        ///     ��һ��������
        /// </summary>
        /// <value>
        ///     The last URL.
        /// </value>
        public string LastUrl { get; set; }

        /// <summary>
        ///     ʹ��ָ���Ĳ������г�ʼ��
        /// </summary>
        /// <param name="param">The parameter.</param>
        public void Init(TParam param)
        {
            this.InnerParam = param;

            this.NextUrl = this.InitFirstUrl(param);

            this.HasMore = true;
        }

        /// <summary>
        ///     Setups the specified parameter.
        /// </summary>
        /// <param name="param">The parameter.</param>
        public override void Init(IParameter param)
        {
            base.Init(param);

            var tParam = Activator.CreateInstance<TParam>();
            tParam.CopyFrom(param);

            this.Init(tParam);
        }

        /// <summary>
        ///     Moves the next.
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            if (!this.HasMore)
            {
                return false;
            }

            this.HtmlSource = this.MoveToNextPage();

            this.NextUrl = this.ParseNextUrl();

            this.CurrentPage = this.ParseCurrentPage();

            this.CountPage = this.ParseCountPage();

            this.Current = this.ParseCurrentItems();

            this.HasMore = this.DetectHasMore();

            this.UpdateResultRankInfo(this.Current, this.CurrentPage);

            return true;
        }

        /// <summary>
        ///     ����������Ϣ
        /// </summary>
        /// <param name="items">The current.</param>
        /// <param name="page">The current page.</param>
        private void UpdateResultRankInfo(TResut[] items, int page)
        {
            Array.ForEach(items, item => this.SetResultSearchPageIndex(item, page, false));

            for (var i = 0; i < items.Length; i++)
            {
                this.SetResultSearchPageRank(items[i], i + 1, false);
            }
        }

        /// <summary>
        ///     Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator<TResut[]> IEnumerable<TResut[]>.GetEnumerator()
        {
            return this;
        }

        /// <summary>
        ///     ����Ƿ��и��������
        /// </summary>
        /// <returns></returns>
        protected virtual bool DetectHasMore()
        {
            return !StringExtension.IsNullOrWhiteSpace(this.NextUrl);
        }

        /// <summary>
        /// ���ص�ǰ��ҳ�������
        /// </summary>
        /// <param name="nextUrl">The next URL.</param>
        /// <param name="postData">The post data.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="currentUrl">The current URL.</param>
        /// <returns></returns>
        protected virtual string GetMainWebContent(string nextUrl, byte[] postData, ref string cookies, string currentUrl)
        {
            return this.GetWebContent(nextUrl, postData, ref cookies, currentUrl);
        }

        /// <summary>
        ///     ����ָ�� url ��ҳ������
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The post data.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="refere">The refere.</param>
        /// <param name="isAjax">�ǲ���ajax����.</param>
        /// <returns></returns>
        protected string GetWebContent(
            string url, 
            byte[] postData, 
            ref string cookies, 
            string refere = "", 
            bool isAjax = false)
        {
            var param = WebRequestCtrl.GetWebContentParam.Default;
            param.Refere = refere;
            param.IsAjax = isAjax;

            cookies = cookies ?? string.Empty;
            var webContent = WebRequestCtrl.GetWebContent(url, null, ref cookies, 1, param);

            return webContent;
        }

        /// <summary>
        ///     ����ָ�� url ��ҳ������
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="keepCookies">if set to <c>true</c> [keep cookies].</param>
        /// <returns></returns>
        protected string GetWebContent(string url, bool keepCookies = true)
        {
            var cookies = this.Cookies;
            var webContent = this.GetWebContent(url, null, ref cookies);

            if (keepCookies)
            {
                this.Cookies = cookies;
            }

            return webContent;
        }

        /// <summary>
        ///     Ϊ��һҳ����ʼ��׼��
        /// </summary>
        /// <param name="param">The parameter.</param>
        protected abstract string InitFirstUrl(TParam param);

        /// <summary>
        ///     �ƶ�����һҳ
        /// </summary>
        /// <returns>System.String.</returns>
        protected virtual string MoveToNextPage()
        {
            var currentUrl = this.CurrentUrl;
            var nextUrl = this.NextUrl;

            if (StringExtension.IsNullOrWhiteSpace(nextUrl))
            {
                throw new NotSupportedException(@"û��ָ����Ҫ���ʵ�ҳ������");
            }

            var cookies = this.Cookies;
            var webContent = this.GetMainWebContent(nextUrl, null, ref cookies, currentUrl);

            this.VerifyWebContent(webContent, cookies);

            this.Cookies = cookies;
            this.LastUrl = currentUrl;
            this.CurrentUrl = this.NextUrl;

            return webContent;
        }

        /// <summary>
        ///     ��������ҳ��
        /// </summary>
        /// <returns></returns>
        protected virtual int ParseCountPage()
        {
            return -1;
        }

        /// <summary>
        ///     ��������ǰֵ
        /// </summary>
        /// <returns></returns>
        protected abstract TResut[] ParseCurrentItems();

        /// <summary>
        ///     ��������ǰҳ��
        /// </summary>
        /// <returns></returns>
        protected virtual int ParseCurrentPage()
        {
            return -1;
        }

        /// <summary>
        ///     ��������һҳ�ĵ�ַ��Ĭ�ϵ�û����һҳ��ʱ��ö��ֹͣ
        /// </summary>
        /// <returns></returns>
        protected abstract string ParseNextUrl();

        /// <summary>
        ///     ���ҳ�������Ƿ���ȷ
        /// </summary>
        /// <param name="webContent">Content of the web.</param>
        /// <param name="cookies">The cookies.</param>
        protected virtual void VerifyWebContent(string webContent, string cookies)
        {
        }
    }
}