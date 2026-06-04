document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.highlight').forEach(block => {
    if (block.querySelector('.copy-code-button')) return;
    const btn = document.createElement('button');
    btn.className = 'copy-code-button';
    btn.type = 'button';
    btn.textContent = 'Copy';
    btn.addEventListener('click', async () => {
      const code = block.querySelector('code') || block.querySelector('pre');
      if (!code) return;
      try {
        await navigator.clipboard.writeText(code.innerText);
        btn.textContent = 'Copied!';
        btn.classList.add('copied');
        setTimeout(() => { btn.textContent = 'Copy'; btn.classList.remove('copied'); }, 1500);
      } catch (e) { btn.textContent = 'Error'; setTimeout(() => btn.textContent = 'Copy', 1500); }
    });
    block.appendChild(btn);
  });
});
