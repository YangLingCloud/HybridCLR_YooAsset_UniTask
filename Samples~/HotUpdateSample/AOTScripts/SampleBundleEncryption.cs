using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using YooAsset;

/// <summary>
/// 示例资源包加密与解密服务集合，用于演示 YooAsset 加密接口在 AOT 侧的接入方式。
/// </summary>
public class SampleBundleEncryption
{
    
}

/// <summary>
/// 文件流加密方式
/// </summary>
public class FileStreamTestEncryption : IEncryptionServices
{
    /// <summary>
    /// 使用异或方式对匹配规则的资源包进行整包加密。
    /// </summary>
    public EncryptResult Encrypt(EncryptFileInfo fileInfo)
    {
        // 说明：对TestRes3资源目录进行加密
        if (fileInfo.BundleName.Contains("_testres3_"))
        {
            // 读取完整文件到内存并逐字节异或，适合演示逻辑，不适合超大资源包直接照搬。
            var fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
            for (int i = 0; i < fileData.Length; i++)
            {
                fileData[i] ^= BundleStream.KEY;
            }

            EncryptResult result = new EncryptResult();
            result.Encrypted = true;
            result.EncryptedData = fileData;
            return result;
        }
        else
        {
            EncryptResult result = new EncryptResult();
            result.Encrypted = false;
            return result;
        }
    }
}

/// <summary>
/// 文件偏移加密方式
/// </summary>
public class FileOffsetTestEncryption : IEncryptionServices
{
    /// <summary>
    /// 使用文件头偏移方式对匹配规则的资源包进行加密。
    /// </summary>
    public EncryptResult Encrypt(EncryptFileInfo fileInfo)
    {
        // 说明：对TestRes3资源目录进行加密
        if (fileInfo.BundleName.Contains("_testres3_"))
        {
            // 在文件头预留固定偏移，使真实 AssetBundle 数据从 offset 位置开始。
            int offset = 32;
            byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
            var encryptedData = new byte[fileData.Length + offset];
            Buffer.BlockCopy(fileData, 0, encryptedData, offset, fileData.Length);

            EncryptResult result = new EncryptResult();
            result.Encrypted = true;
            result.EncryptedData = encryptedData;
            return result;
        }
        else
        {
            EncryptResult result = new EncryptResult();
            result.Encrypted = false;
            return result;
        }
    }
}


/// <summary>
/// 资源文件解密流
/// </summary>
public class BundleStream : FileStream
{
    public const byte KEY = 64;

    /// <summary>
    /// 创建用于 AssetBundle.LoadFromStream 的解密文件流。
    /// </summary>
    public BundleStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
    {
    }

    /// <summary>
    /// 创建基础解密文件流。
    /// </summary>
    public BundleStream(string path, FileMode mode) : base(path, mode)
    {
    }

    /// <summary>
    /// 读取文件数据时执行异或解密。
    /// </summary>
    public override int Read(byte[] array, int offset, int count)
    {
        var index = base.Read(array, offset, count);
        // 示例直接处理缓冲区全部字节，与上面的异或加密保持对称。
        for (int i = 0; i < array.Length; i++)
        {
            array[i] ^= KEY;
        }
        return index;
    }
}

/// <summary>
/// 资源文件流解密类
/// </summary>
public class FileStreamTestDecryption : IDecryptionServices
{
    /// <summary>
    /// 同步方式获取解密的资源包对象
    /// 注意：加载流对象在资源包对象释放的时候会自动释放
    /// </summary>
    DecryptResult IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo)
    {
        // 托管流会交给 AssetBundle 持有，资源包释放时 YooAsset 会释放该流。
        BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = bundleStream;
        decryptResult.Result = AssetBundle.LoadFromStream(bundleStream, fileInfo.FileLoadCRC, GetManagedReadBufferSize());
        return decryptResult;
    }

    /// <summary>
    /// 异步方式获取解密的资源包对象
    /// 注意：加载流对象在资源包对象释放的时候会自动释放
    /// </summary>
    DecryptResult IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo)
    {
        // 异步加载与同步加载使用相同解密流，只是返回 AssetBundleCreateRequest。
        BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = bundleStream;
        decryptResult.CreateRequest = AssetBundle.LoadFromStreamAsync(bundleStream, fileInfo.FileLoadCRC, GetManagedReadBufferSize());
        return decryptResult;
    }

    /// <summary>
    /// 获取解密的字节数据
    /// </summary>
    byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// 获取解密的文本数据
    /// </summary>
    string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo)
    {
        throw new System.NotImplementedException();
    }

    private static uint GetManagedReadBufferSize()
    {
        // 读取缓冲区大小会影响流式解密性能，示例使用 1KB 便于理解。
        return 1024;
    }
}

/// <summary>
/// 资源文件偏移解密类
/// </summary>
public class FileOffsetTestDecryption : IDecryptionServices
{
    /// <summary>
    /// 同步方式获取解密的资源包对象
    /// 注意：加载流对象在资源包对象释放的时候会自动释放
    /// </summary>
    DecryptResult IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo)
    {
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = null;
        // 偏移加密不需要托管解密流，Unity 直接从指定偏移读取真实 AssetBundle 数据。
        decryptResult.Result = AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, GetFileOffset());
        return decryptResult;
    }

    /// <summary>
    /// 异步方式获取解密的资源包对象
    /// 注意：加载流对象在资源包对象释放的时候会自动释放
    /// </summary>
    DecryptResult IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo)
    {
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = null;
        // 异步偏移加载同样依赖固定文件头偏移。
        decryptResult.CreateRequest = AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, GetFileOffset());
        return decryptResult;
    }

    /// <summary>
    /// 获取解密的字节数据
    /// </summary>
    byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// 获取解密的文本数据
    /// </summary>
    string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo)
    {
        throw new System.NotImplementedException();
    }

    private static ulong GetFileOffset()
    {
        // 必须与 FileOffsetTestEncryption 中写入的 offset 保持一致。
        return 32;
    }
}

/// <summary>
/// WebGL平台解密类
/// 注意：WebGL平台支持内存解密
/// </summary>
public class WebFileStreamTestDecryption : IWebDecryptionServices
{
    /// <summary>
    /// WebGL 平台内存解密入口，将下载到内存的资源包数据解密后加载为 AssetBundle。
    /// </summary>
    public WebDecryptResult LoadAssetBundle(WebDecryptFileInfo fileInfo)
    {
        /*
        byte[] copyData = new byte[fileInfo.FileData.Length];
        Buffer.BlockCopy(fileInfo.FileData, 0, copyData, 0, fileInfo.FileData.Length);

        for (int i = 0; i < copyData.Length; i++)
        {
            copyData[i] ^= BundleStream.KEY;
        }

        WebDecryptResult decryptResult = new WebDecryptResult();
        decryptResult.Result = AssetBundle.LoadFromMemory(copyData);
        return decryptResult;
        */

        for (int i = 0; i < fileInfo.FileData.Length; i++)
        {
            // WebGL 不能使用普通文件流，示例直接在内存字节数组上执行异或解密。
            fileInfo.FileData[i] ^= BundleStream.KEY;
        }

        WebDecryptResult decryptResult = new WebDecryptResult();
        decryptResult.Result = AssetBundle.LoadFromMemory(fileInfo.FileData);
        return decryptResult;
    }
}
