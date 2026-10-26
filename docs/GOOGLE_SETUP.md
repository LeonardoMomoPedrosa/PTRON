# Google Services Setup Guide

This guide explains how to configure Google Search (SEO), Google Analytics, Google Tag Manager, and Google Ads for the PTRON/Makelectron application.

## Table of Contents

1. [Google Search Console (SEO)](#google-search-console-seo)
2. [Google Analytics 4 (GA4)](#google-analytics-4-ga4)
3. [Google Tag Manager (GTM)](#google-tag-manager-gtm)
4. [Google Ads](#google-ads)
5. [Configuration](#configuration)

---

## Google Search Console (SEO)

### What's Already Implemented

✅ **robots.txt** - Located at `/wwwroot/robots.txt`
✅ **Dynamic sitemap.xml** - Auto-generated at `/sitemap.xml`
✅ **Meta descriptions** on all pages
✅ **Open Graph tags** for social media sharing
✅ **Twitter Card tags** for Twitter/X sharing
✅ **Structured data (Schema.org JSON-LD)** - Organization and WebApplication
✅ **Canonical URLs** on all pages
✅ **hreflang tags** for multilingual support (pt-BR, en, es)

### Setup Steps

1. **Verify Site Ownership**
   - Go to [Google Search Console](https://search.google.com/search-console)
   - Add property: `https://makelectron.com`
   - Choose verification method (HTML file, DNS, or meta tag)
   - If using meta tag, add to `_Layout.cshtml`:
     ```html
     <meta name="google-site-verification" content="YOUR_VERIFICATION_CODE" />
     ```

2. **Submit Sitemap**
   - In Search Console, go to Sitemaps
   - Submit: `https://makelectron.com/sitemap.xml`

3. **Update robots.txt**
   - Edit `/wwwroot/robots.txt`
   - Update the Sitemap URL if your domain is different

4. **Configure Site URL**
   - Set `SiteUrl` in `appsettings.json` or environment variable:
     ```json
     "SiteUrl": "https://makelectron.com"
     ```
   - Or via environment variable:
     ```bash
     export PTRON_SiteUrl="https://makelectron.com"
     ```

---

## Google Analytics 4 (GA4)

### Setup Steps

1. **Create GA4 Property**
   - Go to [Google Analytics](https://analytics.google.com/)
   - Create new property
   - Get your Measurement ID (format: `G-XXXXXXXXXX`)

2. **Configure in Application**
   
   **Option A: appsettings.json**
   ```json
   "GoogleAnalytics": {
     "MeasurementId": "G-XXXXXXXXXX",
     "AnonymizeIp": true
   }
   ```

   **Option B: Environment Variables**
   ```bash
   export PTRON_GoogleAnalytics__MeasurementId="G-XXXXXXXXXX"
   export PTRON_GoogleAnalytics__AnonymizeIp="true"
   ```

3. **Verify Installation**
   - The `<GoogleAnalytics />` component is already added to `WelcomePage.razor`
   - Check browser console for `gtag` function
   - Use [Google Tag Assistant](https://tagassistant.google.com/) extension

### Features Enabled

- Automatic page view tracking
- IP anonymization (GDPR compliant)
- Cookie consent ready

---

## Google Tag Manager (GTM)

Google Tag Manager allows you to manage all tracking tags from one place without code changes.

### Setup Steps

1. **Create GTM Container**
   - Go to [Google Tag Manager](https://tagmanager.google.com/)
   - Create new container
   - Choose "Web" as target platform
   - Get your Container ID (format: `GTM-XXXXXX`)

2. **Configure in Application**

   **Option A: appsettings.json**
   ```json
   "GoogleTagManager": {
     "ContainerId": "GTM-XXXXXX"
   }
   ```

   **Option B: Environment Variables**
   ```bash
   export PTRON_GoogleTagManager__ContainerId="GTM-XXXXXX"
   ```

3. **Configure Tags in GTM**
   - Add Google Analytics 4 tag
   - Add Google Ads conversion tracking
   - Add remarketing tags
   - Configure triggers (page views, form submissions, button clicks)

4. **Verify Installation**
   - The `<GoogleTagManager />` component is already added to `WelcomePage.razor`
   - Use GTM Preview mode to test
   - Check browser Network tab for GTM requests

### Recommended Tags to Add

- Google Analytics 4 Configuration
- Google Ads Conversion Tracking
- Google Ads Remarketing
- Custom events (signup, production, etc.)

---

## Google Ads

### Setup Steps

1. **Create Google Ads Account**
   - Go to [Google Ads](https://ads.google.com/)
   - Create an account and campaign

2. **Get Conversion ID**
   - Go to Tools → Conversions
   - Create conversion action (e.g., "Account Signup")
   - Get your Conversion ID (format: `AW-XXXXXXXXX`)
   - Get conversion labels for each action

3. **Configure in Application**

   **Option A: appsettings.json**
   ```json
   "GoogleAds": {
     "ConversionId": "AW-XXXXXXXXX",
     "EnableRemarketing": true,
     "EnableEnhancedConversions": false
   }
   ```

   **Option B: Environment Variables**
   ```bash
   export PTRON_GoogleAds__ConversionId="AW-XXXXXXXXX"
   export PTRON_GoogleAds__EnableRemarketing="true"
   export PTRON_GoogleAds__EnableEnhancedConversions="false"
   ```

4. **Track Conversions**

   The `<GoogleAds />` component is already added to the welcome page for remarketing.

   To track specific conversions (e.g., signup), add to the relevant page:
   ```razor
   <GoogleAdsConversion 
       ConversionLabel="YOUR_CONVERSION_LABEL"
       Value="0"
       Currency="BRL"
       TransactionId="@uniqueId"
   />
   ```

   **Example: Track Signup Conversion**
   Add to `Cadastro.razor` after successful signup:
   ```razor
   @if (_done)
   {
       <GoogleAdsConversion 
           ConversionLabel="signup"
           Value="0"
           Currency="BRL"
       />
   }
   ```

5. **Enable Enhanced Conversions** (Optional)
   - Provides better conversion tracking
   - Requires hashed user data (email, phone)
   - Set `EnableEnhancedConversions: true` in config
   - Implement user data hashing in conversion tracking

---

## Configuration Summary

### Complete Configuration Example

**appsettings.json:**
```json
{
  "SiteUrl": "https://makelectron.com",
  "GoogleTagManager": {
    "ContainerId": "GTM-XXXXXX"
  },
  "GoogleAnalytics": {
    "MeasurementId": "G-XXXXXXXXXX",
    "AnonymizeIp": true
  },
  "GoogleAds": {
    "ConversionId": "AW-XXXXXXXXX",
    "EnableRemarketing": true,
    "EnableEnhancedConversions": false
  }
}
```

**Environment Variables (Production):**
```bash
export PTRON_SiteUrl="https://makelectron.com"
export PTRON_GoogleTagManager__ContainerId="GTM-XXXXXX"
export PTRON_GoogleAnalytics__MeasurementId="G-XXXXXXXXXX"
export PTRON_GoogleAnalytics__AnonymizeIp="true"
export PTRON_GoogleAds__ConversionId="AW-XXXXXXXXX"
export PTRON_GoogleAds__EnableRemarketing="true"
export PTRON_GoogleAds__EnableEnhancedConversions="false"
```

---

## Testing

### Local Testing

1. **Check Components Load**
   ```bash
   dotnet run --project src/PTRON
   ```
   Open browser, view page source, verify:
   - Meta tags present
   - GTM script present (if configured)
   - GA4 script present (if configured)

2. **Test Sitemap**
   - Visit: `http://localhost:5083/sitemap.xml`
   - Verify XML is valid

3. **Test robots.txt**
   - Visit: `http://localhost:5083/robots.txt`
   - Verify content

### Production Testing

1. **Google Tag Assistant**
   - Install [Google Tag Assistant](https://chrome.google.com/webstore/detail/tag-assistant-legacy-by-g/kejbdjndbnbjgmefkgdddjlbokphdefk)
   - Visit your site
   - Check all tags fire correctly

2. **Search Console**
   - Use URL Inspection tool
   - Check mobile usability
   - Review coverage reports

3. **Google Analytics**
   - Check Real-time reports
   - Verify page views tracking

4. **Google Ads**
   - Use Google Ads Tag Preview
   - Test conversion tracking

---

## SEO Best Practices Implemented

✅ **Semantic HTML** - Proper heading hierarchy (h1, h2, h3)
✅ **Alt text** on images
✅ **Mobile responsive** - Viewport meta tag
✅ **Fast loading** - Preconnect to external domains
✅ **HTTPS** - Enforced in production
✅ **Sitemap** - Dynamic, auto-updated
✅ **robots.txt** - Properly configured
✅ **Structured data** - Schema.org JSON-LD
✅ **Open Graph** - Social sharing optimization
✅ **Canonical URLs** - Prevent duplicate content
✅ **hreflang tags** - Multilingual SEO
✅ **No-index for private pages** - Login, dashboard pages

---

## Privacy & GDPR Compliance

- **IP Anonymization** enabled by default in GA4
- **Cookie consent** - Consider adding cookie banner (not implemented)
- **Privacy Policy** - Add link to privacy policy page
- **Data Protection** - Review data collection practices

---

## Troubleshooting

### Meta Tags Not Showing
- Check if components are imported in `_Imports.razor`
- Verify configuration values are set
- Check browser view source (not inspect element)

### GTM Not Loading
- Verify Container ID format (`GTM-XXXXXX`)
- Check browser console for errors
- Verify environment variable format

### Conversions Not Tracking
- Check Conversion ID format (`AW-XXXXXXXXX`)
- Verify conversion label matches Google Ads
- Test with Google Tag Assistant
- Check Network tab for gtag requests

### Sitemap Empty
- Check SiteUrl configuration
- Verify controller is registered
- Check application logs for errors

---

## Additional Resources

- [Google Search Console Help](https://support.google.com/webmasters)
- [Google Analytics 4 Documentation](https://support.google.com/analytics)
- [Google Tag Manager Help](https://support.google.com/tagmanager)
- [Google Ads Help](https://support.google.com/google-ads)
- [Schema.org Documentation](https://schema.org/)

---

## Support

For issues or questions:
- Check application logs: `dotnet run --project src/PTRON`
- Review browser console for errors
- Test with Google's tools (Tag Assistant, Rich Results Test)
- Open an issue on GitHub
