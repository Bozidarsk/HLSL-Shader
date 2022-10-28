using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;

// https://gist.github.com/oguna/624969e732a868ec17f05694012c1b63
// https://stackoverflow.com/questions/63281715/how-to-execute-hlsl-from-c

[System.Serializable]
public struct Color 
{
    public float r;
    public float g;
    public float b;
    public float a;
}

static class Compute
{
    public static void Start(string[] args)
    {
        int WIDTH = 1920;
        int HEIGHT = 1080;
        int NUMTHREADS = 8;
        int SIZEOFELEMENT = sizeof(float) * 4; // The size of a single element of the input-data in bytes
        Color[] DATA = new Color[WIDTH * HEIGHT];

        if (args.Length == 2) 
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(System.Drawing.Image.FromFile(args[1]));
            for (int y = 0; y < bitmap.Height; y++) 
            {
                for (int x = 0; x < bitmap.Width; x++) 
                {
                    if (y * bitmap.Width + x >= DATA.Length) { break; }

                    Color color;
                    System.Drawing.Color originalColor = bitmap.GetPixel(x, y);
                    color.r = (float)originalColor.R / 255f;
                    color.g = (float)originalColor.G / 255f;
                    color.b = (float)originalColor.B / 255f;
                    color.a = (float)originalColor.A / 255f;
                    DATA[y * bitmap.Width + x] = color;
                }
            }
        }

        // Create device
        Device device = new Device(DriverType.Hardware, DeviceCreationFlags.SingleThreaded);

        // Create compute shader
        CompilationResult bytecode = ShaderBytecode.CompileFromFile(args[0], "CSMain", "cs_5_0"); // (Gotta have the shader-file shader.hlsl be copied to the output directory for this to work)
        ComputeShader shader = new ComputeShader(device, bytecode);
        bytecode.Dispose();

        // Create input buffer that has the input data
        BufferDescription inputDesc = new BufferDescription() 
        {
            SizeInBytes = SIZEOFELEMENT * DATA.Length, // Size of the buffer in bytes
            Usage = ResourceUsage.Default, // Lets the buffer be both written and read by the GPU
            BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
            OptionFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = SIZEOFELEMENT, // The size of each element in bytes
            CpuAccessFlags = CpuAccessFlags.Read // Lets the CPU read this buffer
        };

        SharpDX.Direct3D11.Buffer buffer = SharpDX.Direct3D11.Buffer.Create(device, DATA, inputDesc);
        // Create resource view (Seems to just be needed for the buffer)
        ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription() 
        {
            Format = SharpDX.DXGI.Format.Unknown,
            Dimension = ShaderResourceViewDimension.Buffer,
            Buffer = new ShaderResourceViewDescription.BufferResource() 
            {
                ElementWidth = SIZEOFELEMENT
            }
        };

        ShaderResourceView srvs = new ShaderResourceView(device, buffer, srvDesc);
        // Create access view (Seems to just be needed for the buffer)
        UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription()
        {
            Format = SharpDX.DXGI.Format.Unknown,
            Dimension = UnorderedAccessViewDimension.Buffer,
            Buffer = new UnorderedAccessViewDescription.BufferResource()
            {
                ElementCount = DATA.Length
            }
        };
        
        UnorderedAccessView uavs = new UnorderedAccessView(device, buffer, uavDesc);

        // Set up shader
        DeviceContext context = device.ImmediateContext;
        context.ComputeShader.Set(shader);

        // Set up shader's buffer
        context.ComputeShader.SetConstantBuffer(0, buffer);
        context.ComputeShader.SetShaderResource(0, srvs);
        context.ComputeShader.SetUnorderedAccessView(0, uavs);

        // Execute shader
        context.Dispatch(WIDTH / NUMTHREADS, HEIGHT / NUMTHREADS, 1);

        // Set an array "outputData" equal to the buffer's values
        DataStream ds;
        context.MapSubresource(buffer, MapMode.Read, MapFlags.None, out ds);
        Color[] outputData = ds.ReadRange<Color>(DATA.Length);

        // Dispose stuff
        context.ClearState();
        Utilities.Dispose(ref srvs);
        Utilities.Dispose(ref uavs);
        Utilities.Dispose(ref buffer);
        Utilities.Dispose(ref shader);
        Utilities.Dispose(ref device);


        System.Drawing.Bitmap background = new System.Drawing.Bitmap(WIDTH, HEIGHT);
        for (int y = 0; y < background.Height; y++) 
        {
            for (int x = 0; x < background.Width; x++) 
            {
                System.Drawing.Color color = System.Drawing.Color.FromArgb(
                    ((int)Utils.Math2.Lerp(0f, 255f, outputData[y * WIDTH + x].a) << 24) +
                    ((int)Utils.Math2.Lerp(0f, 255f, outputData[y * WIDTH + x].r) << 16) +
                    ((int)Utils.Math2.Lerp(0f, 255f, outputData[y * WIDTH + x].g) << 8) +
                    (int)Utils.Math2.Lerp(0f, 255f, outputData[y * WIDTH + x].b)
                );

                background.SetPixel(x, y, color);
            }
        }

        Utils.Tools.SetWallpaper(background);
    }
}

static class Fragment 
{
    public static void Start(string[] args)
    {
        int WIDTH = 1920;
        int HEIGHT = 1080;
        int NUMTHREADS = 8;
        int SIZEOFELEMENT = sizeof(float) * 4; // The size of a single element of the input-data in bytes
        Color[] DATA = new Color[WIDTH * HEIGHT];

        // System.Drawing.Bitmap texture = (args.Length == 2) ? 
        // System.Drawing.Bitmap(System.Drawing.Image.FromFile(args[1])) : 
        // System.Drawing.Bitmap(WIDTH, HEIGHT);

        System.Drawing.Bitmap texture;
        if (args.Length == 2) { texture = new System.Drawing.Bitmap(System.Drawing.Image.FromFile(args[1])); }
        else { texture = new System.Drawing.Bitmap(WIDTH, HEIGHT); }

        for (int y = 0; y < texture.Height && args.Length == 2; y++) 
        {
            for (int x = 0; x < texture.Width; x++) 
            {
                if (y * texture.Width + x >= DATA.Length) { break; }

                Color color;
                System.Drawing.Color originalColor = texture.GetPixel(x, y);
                color.r = (float)originalColor.R / 255f;
                color.g = (float)originalColor.G / 255f;
                color.b = (float)originalColor.B / 255f;
                color.a = (float)originalColor.A / 255f;
                DATA[y * texture.Width + x] = color;
            }
        }

        texture.Save("_MainTex.bmp");

        // Create device
        Device device = new Device(DriverType.Hardware, DeviceCreationFlags.SingleThreaded);

        // Create compute shader
        CompilationResult bytecode = ShaderBytecode.CompileFromFile(args[0], "frag", "ps_4_0");

        // ComputeShader shader = new ComputeShader(device, bytecode);
        PixelShader shader = new PixelShader(device, bytecode);
        bytecode.Dispose();

        // Set up shader
        DeviceContext context = device.ImmediateContext;
        context.PixelShader.SetShaderResource(0, SharpDX.Direct3D10.ShaderResourceView.FromMemory(device, main.ObjectToByteArray(texture)));        
        context.PixelShader.Set(shader);

        Utilities.Dispose(ref shader);
        Utilities.Dispose(ref device);

        // Utils.Tools.SetWallpaper(texture);


        /*

        // Create input buffer that has the input data
        BufferDescription inputDesc = new BufferDescription() 
        {
            SizeInBytes = SIZEOFELEMENT * DATA.Length, // Size of the buffer in bytes
            Usage = ResourceUsage.Default, // Lets the buffer be both written and read by the GPU
            BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
            OptionFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = SIZEOFELEMENT, // The size of each element in bytes
            CpuAccessFlags = CpuAccessFlags.Read // Lets the CPU read this buffer
        };

        SharpDX.Direct3D11.Buffer buffer = SharpDX.Direct3D11.Buffer.Create(device, DATA, inputDesc);
        // Create resource view (Seems to just be needed for the buffer)
        ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription() 
        {
            Format = SharpDX.DXGI.Format.Unknown,
            Dimension = ShaderResourceViewDimension.Buffer,
            Buffer = new ShaderResourceViewDescription.BufferResource() 
            {
                ElementWidth = SIZEOFELEMENT
            }
        };

        ShaderResourceView srvs = new ShaderResourceView(device, buffer, srvDesc);
        // Create access view (Seems to just be needed for the buffer)
        UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription()
        {
            Format = SharpDX.DXGI.Format.Unknown,
            Dimension = UnorderedAccessViewDimension.Buffer,
            Buffer = new UnorderedAccessViewDescription.BufferResource()
            {
                ElementCount = DATA.Length
            }
        };
        
        UnorderedAccessView uavs = new UnorderedAccessView(device, buffer, uavDesc);


        // Set up shader's buffer
        context.ComputeShader.SetConstantBuffer(0, buffer);
        context.ComputeShader.SetShaderResource(0, srvs);
        context.ComputeShader.SetUnorderedAccessView(0, uavs);

        // Execute shader
        context.Dispatch(WIDTH / NUMTHREADS, HEIGHT / NUMTHREADS, 1);

        // Set an array "outputData" equal to the buffer's values
        DataStream ds;
        context.MapSubresource(buffer, MapMode.Read, MapFlags.None, out ds);
        Color[] outputData = ds.ReadRange<Color>(DATA.Length);

        // Dispose stuff
        context.ClearState();
        Utilities.Dispose(ref srvs);
        Utilities.Dispose(ref uavs);
        Utilities.Dispose(ref buffer);
        Utilities.Dispose(ref shader);
        Utilities.Dispose(ref device);

        */
    }
}

public static class main 
{
    public static byte[] ObjectToByteArray(object obj)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    public static Object ByteArrayToObject(byte[] array)
    {
        using (var memStream = new MemoryStream())
        {
            var binForm = new BinaryFormatter();
            memStream.Write(array, 0, array.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = binForm.Deserialize(memStream);
            return obj;
        }
    }

    static void Main(string[] args) 
    {
        if (args.Length == 0) { return; }

        Fragment.Start(args);
    }
}