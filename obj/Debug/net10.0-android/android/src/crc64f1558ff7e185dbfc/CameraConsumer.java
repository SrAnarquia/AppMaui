package crc64f1558ff7e185dbfc;


public class CameraConsumer
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		androidx.core.util.Consumer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_accept:(Ljava/lang/Object;)V:GetAccept_Ljava_lang_Object_Handler:AndroidX.Core.Util.IConsumerInvoker, Xamarin.AndroidX.Core\n" +
			"";
		mono.android.Runtime.register ("CommunityToolkit.Maui.Core.CameraConsumer, CommunityToolkit.Maui.Camera", CameraConsumer.class, __md_methods);
	}

	public CameraConsumer ()
	{
		super ();
		if (getClass () == CameraConsumer.class) {
			mono.android.TypeManager.Activate ("CommunityToolkit.Maui.Core.CameraConsumer, CommunityToolkit.Maui.Camera", "", this, new java.lang.Object[] {  });
		}
	}

	public void accept (java.lang.Object p0)
	{
		n_accept (p0);
	}

	private native void n_accept (java.lang.Object p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
