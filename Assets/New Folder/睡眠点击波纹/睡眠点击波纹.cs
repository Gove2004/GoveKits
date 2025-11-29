using UnityEngine;

public class WaterRippleClick : MonoBehaviour
{
    private Material mat;

    void Start()
    {
        // 获取材质实例
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        // 当按下鼠标左键时
        if (Input.GetMouseButtonDown(0))
        {
            // 1. 获取鼠标点击的屏幕位置
            Vector3 mousePos = Input.mousePosition;
            
            // 2. 转换为世界坐标
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0;

            // 3. 转换为图片的 UV 坐标 (0-1)
            // 这一步有点数学，我们用最简单的方法：利用包围盒
            Bounds bounds = GetComponent<Renderer>().bounds;
            
            // 计算点击点在包围盒内的相对位置
            float u = (worldPos.x - bounds.min.x) / bounds.size.x;
            float v = (worldPos.y - bounds.min.y) / bounds.size.y;

            // 4. 传给 Shader
            mat.SetVector("_Center", new Vector4(u, v, 0, 0));
            mat.SetFloat("_StartTime", Time.timeSinceLevelLoad); // 记录当前时间
        }
    }
}