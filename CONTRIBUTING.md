# Contributing to HdrHistogram.NET

Thank you for your interest in contributing to HdrHistogram.NET!

## Fork Workflow

This repository uses a **fork workflow** for contributions:

```
upstream (HdrHistogram/HdrHistogram.NET)  <-- PRs target here
    ^
    |  Pull Request
    |
origin (YourUsername/HdrHistogram.NET)    <-- Push feature branches here
    ^
    |  git push
    |
local repository                          <-- Your development work
```

### Initial Setup

1. **Fork** the repository on GitHub
2. **Clone** your fork locally:
   ```bash
   git clone https://github.com/YourUsername/HdrHistogram.NET.git
   cd HdrHistogram.NET
   ```
3. **Add upstream** remote:
   ```bash
   git remote add upstream https://github.com/HdrHistogram/HdrHistogram.NET.git
   ```
4. **Install git hooks** (recommended):
   ```bash
   git config core.hooksPath .githooks
   ```

### Making Changes

1. **Sync with upstream** before starting work:
   ```bash
   git fetch upstream
   git checkout main
   git merge upstream/main
   ```

2. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes** and commit:
   ```bash
   git add .
   git commit -m "Description of changes"
   ```

4. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

5. **Create a Pull Request** on GitHub from your fork to upstream

### Branch Protection

Direct pushes to `main` or `master` branches are discouraged. The included git hooks will warn you if you attempt this.

To install the hooks:
```bash
git config core.hooksPath .githooks
```

### Before Submitting a PR

- Ensure your code builds: `dotnet build`
- Run tests: `dotnet test`
- Keep commits focused and well-described
- Reference any related issues in your PR description

## Code Style

- Follow existing code conventions in the repository
- Use meaningful variable and method names
- Add XML documentation for public APIs

## Questions?

Feel free to open an issue for discussion before starting significant work.
