package com.unity3d.player;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.util.Log;
import android.webkit.WebView;
import android.webkit.WebViewClient;

public class PrivacyActivity extends Activity implements DialogInterface.OnClickListener {
    private static final String TAG = "PrivacyActivity";
    private static final String PREFS_NAME = "PlayerPrefs";
    private static final String ACCEPTED_KEY = "PrivacyAcceptedKey";

    // 隐私政策内容
final String privacyContext =
    "隐私政策\r\n" + //
                "感谢您信任并选择我们的游戏《圆头猫大作战》\r\n" + //
                "我们非常重视您的个人信息和隐私保护，为了保障您的个人权益，在您进入游戏前，请务必审核阅读隐私政策内的所有条款，尤其是：\r\n" + //
                "1、为了为您推荐您可能感兴趣的内容/商业广告信息，可能需要您开启地理位置权限，您有权拒绝授权。\r\n" + //
                "2、我们可能会申请存储权限，用于存放广告下载的应用、读取图片/视频用于分享。\r\n" + //
                "3、我们可能会申请电话权限，以保障软件服务的安全运营及效率、完成广告和信息的推送和统计。请您放心，我们不会通过该权限获取您的电话号码、通话内容，也不会在您不知情的情况下拨打电话。\r\n" + //
                "4、如您是未成年人，请您和您的监护人仔细阅读本隐私政策，并在征得您的监护人授权同意的前提下使用我们的服务或向我们提供个人信息。\r\n" + //
                "1.我们收集和使用的个人信息\r\n" + //
                "（一）为什么要收集您的信息和数据\r\n" + //
                "为了完成本游戏正常的服务功能、调查分析问题，持续提高服务质量，我们会收集您的部分个人信息。同时，我们会根据国家法律法规及政府主管部门的要求，在依法合规的情况下，收集您的信息和数据。\r\n" + //
                "（二）我们将收集您的哪些个人信息及目的：\r\n" + //
                "为了优化我们的应用及向您提供更好的服务，我们需要收集：\r\n" + //
                "（1）设备硬件信息（包括硬件序列号、设备MAC地址、机型、系统版本）、应用安装列表（包括应用ID、包名、版本）、IP地址、设备标识符(IMEI、IMSI、android ID、oaid)。\r\n" + //
                "以上信息用于识别服务中的安全风险，了解产品适配性，保障产品基础功能的正常运行。\r\n" + //
                "（2）游戏日志：应用使用情况、在线时长、物品日志。\r\n" + //
                "当您使用我们的游戏服务时，我们会收集您的以上信息，用于标记您的游戏身份标记以及游戏运营统计分析、游戏内容分析，以提升您的游戏体验。\r\n" + //
                "在您使用本游戏产品服务的过程中，我们会按照如下方式收集您在使用服务时主动提供的或因为使用服务而产生的信息，用以向您提供、优化我们的服务以及保障您的账号安全：\r\n" + //
                "1.1 游戏接入了第三方广告SDK，同时为保障您正常使用我们的服务，维护游戏基础功能的正常运行，根据您的设备终端和网络状态优化本游戏产品性能，提升您的游戏体验并保障您的账号安全，我们会收集您的设备名称、设备类型、设备型号和版本、操作系统、系统属性、IP地址、运行中的进程、软件安装列表、通讯录、短信、运营商信息、Wi-Fi状态/参数，设备识别符（包括IMEI、IMSI、MAC 地址、Android ID、硬件序列号）、应用ID、使用GPS获取定位、使用网络获取定位信息。\r\n" + //
                "具体信息如下：\r\n" + //
                "SDK名称：（字节跳动）穿山甲广告SDK\r\n" + //
                "收集使用目的：提供相关广告服务，用于广告投放与监测归因\r\n" + //
                "涉及个人信息范围：设备信息：设备品牌、型号、软件系统版本、分辨率、网络信号强度、IP地址、设备语言、传感器信息等基础信息、AndroidID、无线网SSID名称、WiFi路由器MAC地址、设备的MAC地址、设备标识符（如IMEI、OAID、IMSI、ICCID、GAID（仅GMS服务）、MEID、设备序列号build_serial、IDFA）、开发者应用名、应用包名、版本号、应用前后台状态、应用列表信息\r\n" + //
                "第三方机构名称：字节跳动公司\r\n" + //
                "SDK名称：(腾讯)优量汇广告SDK\r\n" + //
                "收集使用目的：提供相关广告服务，用于广告投放与监测归因\r\n" + //
                "涉及个人信息范围：位置信息、设备制造商、品牌、设备型号、操作系统版本、屏幕分辨率、屏幕方向、屏幕DPI、IP地址、时区、网络类型、运营商、磁力、加速度、重力、陀螺仪传感器、OAID、IMEI、Android ID、IDFV、 IDFA、应用的包名、版本号、进程名称、运行状态、可疑行为、应用安装信息、产品交互数据、广告数据、崩溃数据、性能数据\r\n" + //
                "第三方机构名称：腾讯公司\r\n" + //
                "1.2 为收集上述信息以实现相应的游戏功能和业务场景，我们可能在具体业务场景下向您申请部分系统权限，您可以自主选择是否授权。这些系统权限包括：\r\n" + //
                "（1）电话权限（获取手机识别码，您授予该权限，将同时授予获取手机识别码、拨打电话、管理通话的权限）：应用程序基础服务(程序版本更新，用户行为分析)需要识别您的设备身份，需要调用您的电话权限获取此项信息，除此之外不会基于其他用途进行调用。\r\n" + //
                "（2）存储权限（读写存储，您授予该权限，将同时授予访问设备上的照片、媒体内容和文件的权限）：为了存储必要的应用配置文件以及您所选择下载、更新的内容，或读取您需要上传、同步的内容，需要调用您的存储权限。\r\n" + //
                "（3）定位权限（访问粗略/精准定位）：当您使用我们的应用程序时，我们会显示相关的广告，为此，我们须使用您的位置信息，为了能够正常向您提供功能，在后台运行的情况下，也会获取位置信息。对于不同地区，我们会使用不同的行为广告。如需了解广告需获取的权限和用途可参考以下SDK详细列表。\r\n" + //
                "拒绝上述权限仅会影响与之关联的特定游戏功能和业务场景，不会影响您使用游戏其他游戏功能。请您理解，由于不同终端设备操作系统的原因，在不同终端上的权限名称会有所区别。您可以通过终端设备【设置】页面进行系统权限的管理。\r\n" + //
                "1.个人信息的存储\r\n" + //
                "2.1 信息存储的方式和期限\r\n" + //
                "我们会通过安全的方式存储您的信息，包括本地存储（例如利用APP进行数据缓存）、数据库和服务器日志。一般情况下，我们只会在为实现服务目的所必需的时间内或法律法规规定的条件下存储您的个人信息。前述期限届满后，我们将对您的个人信息做删除或匿名化处理。我们判断个人信息的储存期限主要参考以下标准并以其中较长者为准：\r\n" + //
                "• 完成您同意使用的业务功能；\r\n" + //
                "• 保证我们为您提供服务的安全和质量；\r\n" + //
                "• 您同意的更长的留存期间；\r\n" + //
                "• 是否存在保留期限的其他规定；\r\n" + //
                "• 法律法规规定需要在特定期限内保存的信息。\r\n" + //
                "2.2 产品或服务停止运营时的通知\r\n" + //
                "当我们的产品或服务发生停止运营的情况时，我们将根据相关法律法规规定进行公告通知，并依法保障您的合法权益。\r\n" + //
                "1.信息安全\r\n" + //
                "3.1 安全保护措施\r\n" + //
                "我们努力为用户的信息安全提供保障，以防止信息的泄露、丢失、不当使用、未经授权访问和披露。我们使用多方位的安全保护措施，以确保用户的个人信息保护处于合理的安全水平，包括技术保护手段、管理制度控制、安全体系保障诸多方面。\r\n" + //
                "我们采用业界领先的技术保护措施。我们使用的技术手段包括但不限于防火墙、加密（例如SSL）、去标识化或匿名化处理、访问控制措施。此外，我们还会不断加强安装在您设备端的软件的安全能力。例如，我们会在您的设备本地完成部分信息加密工作，以巩固安全传输；我们会了解您设备安装的应用信息和运行的进程信息，以预防病毒和木马恶意程序。\r\n" + //
                "我们建立了保障个人信息安全专门的管理制度、流程和组织。例如，我们严格限制访问信息的人员范围，要求他们遵守保密义务并进行审计，违反义务的人员会根据规定进行处罚。我们也会审查该管理制度、流程和组织，以防未经授权的人员擅自访问、使用或披露用户的信息。\r\n" + //
                "我们建议您在使用产品和服务时充分注意对个人信息的保护，我们也会提供多种安全功能来协助您保护自己的个人信息安全。\r\n" + //
                "3.2 安全事件处置措施\r\n" + //
                "若发生个人信息泄露安全事件，我们会启动应急预案，阻止安全事件扩大。安全事件发生后，我们会以公告、推送通知或邮件形式告知您安全事件的基本情况、我们即将或已经采取的处置措施和补救措施，以及我们对您的应对建议。如果难以实现逐一告知，我们将通过公告方式发布警示。\r\n" + //
                "1.您的权利\r\n" + //
                "在您使用本游戏产品服务期间，我们可能会视游戏产品具体情况为您提供相应的操作设置，以便您可以查询、复制、更正、补充、删除或撤回您的相关个人信息。此外，我们还设置了投诉举报渠道，您的意见将会得到及时的处理。\r\n" + //
                "4.1 个人信息收集和使用规则查询\r\n" + //
                "您可以在本产品内的设置界面随时查看产品个人信息收集和使用规则，包括但不限于本指引、以及产品敏感权限使用规则和授权情况。\r\n" + //
                "请您知悉和了解，本指引中所述服务和对应个人信息收集和使用情况，可能因为您选择使用的具体服务，以及您使用的设备型号、系统版本因素存在差异。\r\n" + //
                "4.2 个人信息收集和使用授权变更\r\n" + //
                "您可以通过删除信息、关闭或开启特定产品功能、关闭或开启特定产品获取的设备权限（具体权限因用户使用的设备型号、系统版本不同而存在差异），变更对我们收集和使用您的个人信息的授权范围。\r\n" + //
                "撤回授权后，您可能无法使用该授权对应产品功能，但不影响其他功能的使用。您撤回授权不影响撤回前基于您的授权已经进行的个人信息处理活动的效力。\r\n" + //
                "部分个人信息收集和使用对应我们服务的基础功能，如您希望撤回相关个人信息收集和使用的授权可能导致我们无法为您提供服务，在此情形下，如您仍希望撤回相关授权，您可以通过申请账号注销实现该需求。例如，基于法律法规要求，使用游戏服务必须进行实名认证，如您希望撤回对我们收集实名认证信息的授权，要求我们删除您的实名认证信息，我们可能无法继续为您提供游戏产品基本服务。\r\n" + //
                "5.如何联系我们\r\n" + //
                "如果您对本政策、有任何问题、意见或建议，您可以通过我们客服qq:732904762与我们联系。一般情况下，我们将在五个自然日内回复。\r\n" + //
                "如果您对我们的回复不满意，您还可以通过以下外部途径寻求解决方案：您可以通过向被告所在地有管辖权的人民法院提起诉讼的方式来寻求解决方案。\r\n" ;
                
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        Log.d(TAG, "onCreate: PrivacyActivity started");

        // 如果已经同意过隐私协议，则直接进入Unity Activity
        if (getPrivacyAccepted()) {
            Log.d(TAG, "onCreate: User already accepted privacy policy");
            enterUnityActivity();
            return;
        }

        // 弹出隐私协议对话框
        showPrivacyDialog();
    }

    // 显示隐私协议对话框
    private void showPrivacyDialog() {
        Log.d(TAG, "showPrivacyDialog: Displaying privacy dialog");
        
        WebView webView = new WebView(this);
        webView.getSettings().setJavaScriptEnabled(true); // 启用JavaScript以支持链接跳转
        webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, String url) {
                view.loadUrl(url); // 在WebView内打开链接
                return true;
            }
        });
        webView.loadData(privacyContext, "text/html", "utf-8");
        
        AlertDialog.Builder privacyDialog = new AlertDialog.Builder(this);
        privacyDialog.setCancelable(false); // 禁止通过返回键取消
        privacyDialog.setView(webView);
        privacyDialog.setTitle("提示");
        privacyDialog.setNegativeButton("拒绝", this);
        privacyDialog.setPositiveButton("同意", this);
        privacyDialog.create().show();
    }

    @Override
    public void onClick(DialogInterface dialogInterface, int which) 
    {
        switch (which) {
            case AlertDialog.BUTTON_POSITIVE: // 点击同意按钮
                Log.d(TAG, "onClick: User accepted privacy policy");
                setPrivacyAccepted(true);
                enterUnityActivity();
                finish(); // 关闭当前Activity，防止返回
                break;
            case AlertDialog.BUTTON_NEGATIVE: // 点击拒绝按钮
                Log.d(TAG, "onClick: User declined privacy policy");
                finish(); // 退出应用
                break;
        }
    }

    // 启动Unity Activity
    private void enterUnityActivity() {
        Log.d(TAG, "enterUnityActivity: Starting UnityPlayerActivity");
        Intent unityIntent = new Intent(this, UnityPlayerActivity.class);
        startActivity(unityIntent);
    }

    // 本地存储保存同意隐私协议状态
    private void setPrivacyAccepted(boolean accepted) {
        SharedPreferences.Editor editor = getSharedPreferences(PREFS_NAME, MODE_PRIVATE).edit();
        editor.putBoolean(ACCEPTED_KEY, accepted);
        editor.apply();
        Log.d(TAG, "setPrivacyAccepted: Saved preference: " + accepted);
    }

    // 获取是否已经同意过
    private boolean getPrivacyAccepted() {
        SharedPreferences prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
        boolean accepted = prefs.getBoolean(ACCEPTED_KEY, false);
        Log.d(TAG, "getPrivacyAccepted: " + accepted);
        return accepted;
    }

    // 禁止通过返回键绕过隐私协议
    @Override
    public void onBackPressed() {
        // 直接退出应用，不允许返回
        finish();
    }
}    