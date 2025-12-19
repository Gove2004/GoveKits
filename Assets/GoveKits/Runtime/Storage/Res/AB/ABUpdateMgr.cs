using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ABUpdateMgr : SingletonAutoMono<ABUpdateMgr>
{
    

    //用于存储远端AB包信息的字典 之后 和本地进行对比即可完成 更新 下载相关逻辑
    private Dictionary<string, ABInfo> remoteABInfo = new Dictionary<string, ABInfo>();

    //用于存储本地AB包信息的字典 主要用于和远端信息对比
    private Dictionary<string, ABInfo> localABInfo = new Dictionary<string, ABInfo>();

    //这个是待下载的AB包列表文件 存储AB包的名字
    private List<string> downLoadList = new List<string>();

    //资源服务器IP
    private string serverIP = "ftp://192.168.31.178";

    /// <summary>
    /// 用于检测热更新的函数
    /// </summary>
    /// <param name="overCallBack"></param>
    /// <param name="updateInfoCallBack"></param>
    public void CheckUpdate(UnityAction<bool> overCallBack, UnityAction<string> updateInfoCallBack)
    {
        //为了避免由于上一次报错 而残留信息 所以我们清空它
        remoteABInfo.Clear();
        localABInfo.Clear();
        downLoadList.Clear();

        //1.加载远端资源对比文件
        DownLoadABCompareFile((isOver) =>
        {
            updateInfoCallBack("开始更新资源");
            if (isOver)
            {
                updateInfoCallBack("对比文件下载结束");
                string remoteInfo = File.ReadAllText(Application.persistentDataPath + "/ABCompareInfo_TMP.txt");
                updateInfoCallBack("解析远端对比文件");
                GetRemoteABCompareFileInfo(remoteInfo, remoteABInfo);
                updateInfoCallBack("解析远端对比文件完成");

                //2.加载本地资源对比文件
                GetLocalABCompareFileInfo((isOver) => {
                    if (isOver)
                    {
                        updateInfoCallBack("解析本地对比文件完成");
                        //3.对比他们 然后进行AB包下载
                        updateInfoCallBack("开始对比");
                        foreach (string abName in remoteABInfo.Keys)
                        {
                            //1.判断 哪些资源时新的 然后记录 之后用于下载
                            //这由于本地对比信息中没有叫这个名字的AB包 所以我们记录下载它
                            if (!localABInfo.ContainsKey(abName))
                                downLoadList.Add(abName);
                            //发现本地有同名AB包 然后继续处理
                            else
                            {
                                //2.判断 哪些资源是需要更新的 然后记录 之后用于下载
                                //对比md5码 判断是否需要更新
                                if (localABInfo[abName].md5 != remoteABInfo[abName].md5)
                                    downLoadList.Add(abName);
                                //如果md5码相等 证明是同一个资源 不需要更新

                                //3.判断 哪些资源需要删除
                                //每次检测完一个名字的AB包 就移除本地的信息 那么本地剩下来的信息 就是远端没有的内容
                                //我们就可以把他们删除了
                                localABInfo.Remove(abName);
                            }
                        }
                        updateInfoCallBack("对比完成");
                        updateInfoCallBack("删除无用的AB包文件");
                        //上面对比完了 那么我们就先删除没用的内容 再下载AB包
                        //删除无用的AB包
                        foreach (string abName in localABInfo.Keys)
                        {
                            //如果可读写文件夹中有内容 我们就删除它 
                            //默认资源中的 信息 我们没办法删除
                            if (File.Exists(Application.persistentDataPath + "/" + abName))
                                File.Delete(Application.persistentDataPath + "/" + abName);
                        }
                        updateInfoCallBack("下载和更新AB包文件");
                        //下载待更新列表中的所有AB包
                        //下载
                        DownLoadABFile((isOver) =>
                        {
                            if (isOver)
                            {
                                //下载完所有AB包文件后
                                //把本地的AB包对比文件 更新为最新
                                //把之前读取出来的 远端对比文件信息 存储到 本地 
                                updateInfoCallBack("更新本地AB包对比文件为最新");
                                File.WriteAllText(Application.persistentDataPath + "/ABCompareInfo.txt", remoteInfo);
                            }
                            overCallBack(isOver);
                        }, updateInfoCallBack);
                    }
                    else
                        overCallBack(false);
                });
            }
            else
            {
                overCallBack(false);
            }
        });
    }

    /// <summary>
    /// 下载AB包对比文件
    /// </summary>
    /// <param name="overCallBack"></param>
    public async void DownLoadABCompareFile(UnityAction<bool> overCallBack)
    {
        //1.从资源服务器下载资源对比文件
        // www UnityWebRequest ftp相关api
        Debug.Log(Application.persistentDataPath);
        bool isOver = false;
        int reDownLoadMaxNum = 5;
        //不能在子线程中访问Unity主线程的 Application 所以 在外面声明
        string localPath = Application.persistentDataPath;
        while (!isOver && reDownLoadMaxNum > 0)
        {
            await Task.Run(() => {
                isOver = DownLoadFile("ABCompareInfo.txt", localPath + "/ABCompareInfo_TMP.txt");
            });
            --reDownLoadMaxNum;
        }

        //告诉外部成功与否
        overCallBack?.Invoke(isOver);
    }

    /// <summary>
    /// 获取下载下来的AB包中的信息
    /// </summary>
    public void GetRemoteABCompareFileInfo(string info, Dictionary<string, ABInfo> ABInfo)
    {
        //2.就是获取资源对比文件中的 字符串信息 进行拆分
        //这就不去读取文件了 直接让外部读好了 传进来
        //string info = File.ReadAllText(Application.persistentDataPath + "/ABCompareInfo_TMP.txt");
        string[] strs = info.Split('|');//通过|拆分字符串 把一个个AB包信息拆分出来
        string[] infos = null;
        for (int i = 0; i < strs.Length; i++)
        {
            infos = strs[i].Split(' ');//又把一个AB的详细信息拆分出来
                                       //记录每一个远端AB包的信息 之后 好用来对比
            ABInfo.Add(infos[0], new ABInfo(infos[0], infos[1], infos[2]));
        }
    }

    /// <summary>
    /// 本地AB包对比文件加载 解析信息
    /// </summary>
    public void GetLocalABCompareFileInfo(UnityAction<bool> overCallBack)
    {
        //Application.persistentDataPath;
        //如果可读可写文件夹中 存在对比文件 说明之前我们已经下载更新过了
        if (File.Exists(Application.persistentDataPath + "/ABCompareInfo.txt"))
        {
            StartCoroutine(GetLocalABCOmpareFileInfo("file:///" + Application.persistentDataPath + "/ABCompareInfo.txt", overCallBack));
        }
        //只有当可读可写中没有对比文件时  才会来加载默认资源（第一次进游戏时才会发生）
        else if (File.Exists(Application.streamingAssetsPath + "/ABCompareInfo.txt"))
        {
            string path =
#if UNITY_ANDROID
                Application.streamingAssetsPath;
#else
                "file:///" + Application.streamingAssetsPath;
#endif
            StartCoroutine(GetLocalABCOmpareFileInfo(path + "/ABCompareInfo.txt", overCallBack));
        }
        //如果两个都不进 证明第一次并且没有默认资源 
        else
            overCallBack(true);
    }

    /// <summary>
    /// 协同程序 加载本地信息 并且解析存入字典
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private IEnumerator GetLocalABCOmpareFileInfo(string filePath, UnityAction<bool> overCallBack)
    {
        //通过 UnityWebRequest 去加载本地文件
        UnityWebRequest req = UnityWebRequest.Get(filePath);
        yield return req.SendWebRequest();
        //获取文件成功 继续往下执行
        if (req.result == UnityWebRequest.Result.Success)
        {
            GetRemoteABCompareFileInfo(req.downloadHandler.text, localABInfo);
            overCallBack(true);
        }
        else
            overCallBack(false);
    }

    /// <summary>
    /// 下载待下载列表中的AB包文件
    /// </summary>
    /// <param name="overCallBack"></param>
    /// <param name="updatePro"></param>
    public async void DownLoadABFile(UnityAction<bool> overCallBack, UnityAction<string> updatePro)
    {
        ////1.遍历字典的键 根据文件名 去下载AB包到本地
        //foreach (string name in remoteABInfo.Keys)
        //{
        //    //直接放入 待下载列表中
        //    downLoadList.Add(name);
        //}
        //本地存储的路径 由于多线程不能访问Unity相关的一些内容比如Application 所以声明再外部
        string localPath = Application.persistentDataPath + "/";
        //是否下载成功
        bool isOver = false;
        //下载成功的列表 之后用于移除下载成功的内容
        List<string> tempList = new List<string>();
        //重新下载的最大次数
        int reDownLoadMaxNum = 5;
        //下载成功的资源数
        int downLoadOverNum = 0;
        //这一次下载需要下载多少个资源
        int downLoadMaxNum = downLoadList.Count;
        //while循环的目的 是进行n次重新下载 避免网络异常时 下载失败
        while (downLoadList.Count > 0 && reDownLoadMaxNum > 0)
        {
            for (int i = 0; i < downLoadList.Count; i++)
            {
                isOver = false;
                await Task.Run(() => {
                    isOver = DownLoadFile(downLoadList[i], localPath + downLoadList[i]);
                });
                if (isOver)
                {
                    //2.要知道现在下载了多少 结束与否
                    updatePro(++downLoadOverNum + "/" + downLoadMaxNum);
                    tempList.Add(downLoadList[i]);//下载成功记录下来
                }
            }
            //把下载成功的文件名 从待下载列表中移除
            for (int i = 0; i < tempList.Count; i++)
                downLoadList.Remove(tempList[i]);

            --reDownLoadMaxNum;
        }

        //所有内容都下载完了 告诉外部是否下载完成
        overCallBack(downLoadList.Count == 0);
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="localPath"></param>
    /// <returns></returns>
    private bool DownLoadFile(string fileName, string localPath)
    {
        try
        {
            string pInfo =
#if UNITY_IOS
            "IOS";
#elif UNITY_ANDROID
            "Android";
#else
            "PC";
#endif
            //1.创建一个FTP连接 用于下载
            FtpWebRequest req = FtpWebRequest.Create(new Uri(serverIP + "/AB/" + pInfo + "/" + fileName)) as FtpWebRequest;
            //2.设置一个通信凭证 这样才能下载（如果有匿名账号 可以不设置凭证 但是实际开发中 建议 还是不要设置匿名账号）
            NetworkCredential n = new NetworkCredential("MrTang", "MrTang123");
            req.Credentials = n;
            //3.其它设置
            //  设置代理为null
            req.Proxy = null;
            //  请求完毕后 是否关闭控制连接
            req.KeepAlive = false;
            //  操作命令-下载
            req.Method = WebRequestMethods.Ftp.DownloadFile;
            //  指定传输的类型 2进制
            req.UseBinary = true;
            //4.下载文件
            //  ftp的流对象
            FtpWebResponse res = req.GetResponse() as FtpWebResponse;
            Stream downLoadStream = res.GetResponseStream();
            using (FileStream file = File.Create(localPath))
            {
                //一点一点的下载内容
                byte[] bytes = new byte[2048];
                //返回值 代表读取了多少个字节
                int contentLength = downLoadStream.Read(bytes, 0, bytes.Length);

                //循环下载数据
                while (contentLength != 0)
                {
                    //写入到本地文件流中
                    file.Write(bytes, 0, contentLength);
                    //写完再读
                    contentLength = downLoadStream.Read(bytes, 0, bytes.Length);
                }

                //循环完毕后 证明下载结束
                file.Close();
                downLoadStream.Close();

                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.Log(fileName + "下载失败" + ex.Message);
            return false;
        }

    }


    /// <summary>
    /// AB包信息类
    /// </summary>
    public class ABInfo
    {
        public string name;//AB包名字
        public long size;//AB包大小
        public string md5;//AB包md5码

        public ABInfo(string name, string size, string md5)
        {
            this.name = name;
            this.size = long.Parse(size);
            this.md5 = md5;
        }
    }
}


