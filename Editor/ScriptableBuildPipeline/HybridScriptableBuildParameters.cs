using UnityEditor.Build.Pipeline;
using UnityEngine;
using YooAsset.Editor;
using BuildParameters = YooAsset.Editor.BuildParameters;

public class HybridScriptableBuildParameters : BuildParameters
{
        /// <summary>
        /// 压缩选项
        /// </summary>
        public ECompressOption CompressOption = ECompressOption.Uncompressed;
        
        
        /// <summary>
        /// 热更新DLL打包路径
        /// 应在HybridSetting和YooAssetCollector中同时存在
        /// </summary>
        public string HotUpdateDLLCollectPath;

        /// <summary>
        /// AOTDLL打包路径
        /// 应在HybridSetting和YooAssetCollector中同时存在
        /// </summary>
        public string PatchedAOTDLLCollectPath;
        
        /// <summary>
        /// 从AssetBundle文件头里剥离Unity版本信息
        /// </summary>
        public bool StripUnityVersion = false;

        /// <summary>
        /// 禁止写入类型树结构（可以降低包体和内存并提高加载效率）
        /// </summary>
        public bool DisableWriteTypeTree = false;

        /// <summary>
        /// 忽略类型树变化
        /// </summary>
        public bool IgnoreTypeTreeChanges = true;

        

        /// <summary>
        /// 生成代码防裁剪配置
        /// </summary>
        public bool WriteLinkXML = true;

        /// <summary>
        /// 缓存服务器地址
        /// </summary>
        public string CacheServerHost;

        /// <summary>
        /// 缓存服务器端口
        /// </summary>
        public int CacheServerPort;


        /// <summary>
        /// 内置着色器资源包名称
        /// </summary>
        public string BuiltinShadersBundleName;

        /// <summary>
        /// Mono脚本资源包名称
        /// </summary>
        public string MonoScriptsBundleName;


        /// <summary>
        /// 获取可编程构建管线的构建参数
        /// </summary>
        public BundleBuildParameters GetBundleBuildParameters()
        {
            var targetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(BuildTarget);
            var pipelineOutputDirectory = GetPipelineOutputDirectory();
            var buildParams = new BundleBuildParameters(BuildTarget, targetGroup, pipelineOutputDirectory);

            if (CompressOption == ECompressOption.Uncompressed)
                buildParams.BundleCompression = UnityEngine.BuildCompression.Uncompressed;
            else if (CompressOption == ECompressOption.LZMA)
                buildParams.BundleCompression = UnityEngine.BuildCompression.LZMA;
            else if (CompressOption == ECompressOption.LZ4)
                buildParams.BundleCompression = UnityEngine.BuildCompression.LZ4;
            else
                throw new System.NotImplementedException(CompressOption.ToString());

            if (StripUnityVersion)
                buildParams.ContentBuildFlags |= UnityEditor.Build.Content.ContentBuildFlags.StripUnityVersion;

            if (DisableWriteTypeTree)
                buildParams.ContentBuildFlags |= UnityEditor.Build.Content.ContentBuildFlags.DisableWriteTypeTree;

            buildParams.UseCache = true;
            buildParams.CacheServerHost = CacheServerHost;
            buildParams.CacheServerPort = CacheServerPort;
            buildParams.WriteLinkXML = WriteLinkXML;

            return buildParams;
        }
}
