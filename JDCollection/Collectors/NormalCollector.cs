namespace X.GlodEyes.Collectors
{
    using System.Collections.Generic;

    using X.CommLib.Office;

    /// <summary>
    ///     ͨ�ô�����
    /// </summary>
    public abstract class NormalCollector : Collector
    {
        /// <summary>
        ///     ���ø���Ŀ��ҳ��
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="page">The page.</param>
        /// <param name="overwrite">�ǲ���ǿ��д��</param>
        protected void SetResultSearchPageIndex(IResut result, int page, bool overwrite = true)
        {
            if (!overwrite && result.ContainsKey(@"SearchPageIndex"))
            {
                return;
            }

            result[@"SearchPageIndex"] = page;
        }

        /// <summary>
        ///     ���ø���Ŀ�ڵ�ǰҳ���е�����
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="rank">The rank.</param>
        /// <param name="overwrite">�ǲ���ǿ��д��</param>
        protected void SetResultSearchPageRank(IResut result, int rank, bool overwrite = true)
        {
            if (!overwrite && result.ContainsKey(@"SearchPageRank"))
            {
                return;
            }

            result[@"SearchPageRank"] = rank;
        }

        /// <summary>
        ///     ���²ɼ�����е� keyname ֵ
        ///     ��������� keyNames �� Key ��Ӧ��������������Ϊ Value ��ָ����ֵ����� Value ����Ϊ null����������� Key �����ڣ���ɾ��������
        /// </summary>
        /// <param name="resuts">The resuts.</param>
        /// <param name="keyNames">The key names.</param>
        protected void UpdateResultsKeyNames(IEnumerable<IResut> resuts, IDictionary<string, string> keyNames)
        {
            foreach (var resut in resuts)
            {
                UpdateResultKeyName(resut, keyNames);
            }
        }

        /// <summary>
        ///     ���� KEYNAMES ������ result����ɾ�������ڵ�key
        /// </summary>
        /// <param name="resut">The resut.</param>
        /// <param name="keyNames">The key names.</param>
        private static void UpdateResultKeyName(IDic resut, IDictionary<string, string> keyNames)
        {
            var keys = resut.KeysToArray();
            var removeList = new List<string>();

            foreach (var key in keys)
            {
                string newKey;

                if (keyNames.TryGetValue(key, out newKey))
                {
                    if (StringExtension.IsNullOrWhiteSpace(newKey))
                    {
                        // δ�����key��Ϊ��һ�µġ�
                        newKey = key;
                    }

                    if (StringExtension.Same(newKey, key, true))
                    {
                        // ����¾�keyһ�£�ֱ������
                        continue;
                    }

                    resut[newKey] = resut[key];
                }

                removeList.Add(key);
            }

            removeList.ForEach(key => resut.Remove(key));
        }
    }
}