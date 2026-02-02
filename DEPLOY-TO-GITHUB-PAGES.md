# Deploy to GitHub Pages

Git is initialized and the initial commit is ready. Follow these steps to create the repo and deploy:

## 1. Log in to GitHub (one-time)

```bash
gh auth login -h github.com -p https -w
```

Copy the one-time code, press Enter to open the browser, paste the code, and authorize.

## 2. Create the repo and push

```bash
cd /Users/shray/Downloads/TaskFlow-Backend/TaskFlow

# Create repo (replace TaskAgent with your desired repo name)
gh repo create TaskAgent --public --source=. --push --description "TaskAgent - Vue.js task management with .NET backend"
```

## 3. Enable GitHub Pages

1. Go to your repo on GitHub: `https://github.com/YOUR_USERNAME/TaskAgent`
2. **Settings** → **Pages**
3. Under **Build and deployment** → **Source**, select **GitHub Actions**
4. If you see "GitHub Actions" grayed out, the first workflow run may need to complete successfully to unlock it

## 4. Add API URL (optional)

Repo → **Settings** → **Secrets and variables** → **Actions** → **Variables** → **New repository variable**

- Name: `VITE_API_BASE`
- Value: `https://taskagent-api-staging.nicemeadow-03f2da8a.eastus.azurecontainerapps.io` (or your backend URL)

## 5. Trigger deployment

The workflow runs on push to `main`. If you just pushed in step 2, it should already be running. Check **Actions** in your repo. After it completes, your app will be live at:

**https://YOUR_USERNAME.github.io/TaskAgent/**

If you get 404, ensure **Settings → Pages → Source** is set to **GitHub Actions** (not "Deploy from a branch").

## 6. Enable CORS on the backend

Add your GitHub Pages URL to the backend so it accepts requests:

```bash
# Via Azure Portal: Container App → Application → Environment variables
# Add: Cors__AllowedOrigins = https://YOUR_USERNAME.github.io

# Or re-run setup with CORS:
CORS_ALLOWED_ORIGINS="https://YOUR_USERNAME.github.io;https://YOUR_USERNAME.github.io/TaskAgent" \
  ./Backend/infra/setup-azure.sh
```

---

**Quick copy-paste** (after `gh auth login`):

```bash
cd /Users/shray/Downloads/TaskFlow-Backend/TaskFlow
gh repo create TaskAgent --public --source=. --push --description "TaskAgent - Vue.js task management with .NET backend"
```

Then enable Pages in repo Settings → Pages → Source: **GitHub Actions**.
