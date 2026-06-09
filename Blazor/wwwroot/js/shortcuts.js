window.utaShortcuts = {
    dotNetHelper: null,
    tipsActive: false,
    init: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        
        window.addEventListener('keydown', (e) => {
            if (e.key === 'Alt') {
                e.preventDefault();
                this.tipsActive = !this.tipsActive;
                if (this.tipsActive) {
                    document.body.classList.add('uta-show-keys');
                    if (document.querySelector('.uta-modal')) {
                        document.body.classList.add('uta-modal-open');
                    } else {
                        document.body.classList.remove('uta-modal-open');
                    }
                } else {
                    document.body.classList.remove('uta-show-keys');
                    document.body.classList.remove('uta-modal-open');
                }
                return;
            }

            if (this.tipsActive) {
                const key = e.key.toUpperCase();
                
                const activeModal = document.querySelector('.uta-modal');
                let targetElement = null;

                if (activeModal) {
                    targetElement = activeModal.querySelector(`[data-accesskey="${key}"]`);
                } else {
                    const elements = Array.from(document.querySelectorAll(`[data-accesskey="${key}"]`));
                    targetElement = elements.find(t => !!(t.offsetWidth || t.offsetHeight || t.getClientRects().length));
                }

                if (targetElement) {
                    e.preventDefault();
                    this.tipsActive = false;
                    document.body.classList.remove('uta-show-keys');
                    document.body.classList.remove('uta-modal-open');
                    
                    // Simular click, focus o navegar si es un link
                    if (targetElement.tagName === 'A' && targetElement.href) {
                        window.location.href = targetElement.href;
                    } else if (targetElement.tagName === 'INPUT' || targetElement.tagName === 'SELECT' || targetElement.tagName === 'TEXTAREA') {
                        targetElement.focus();
                    } else {
                        targetElement.click();
                    }
                }
            }

            // Teclas de Función Globales (F1-F12) y Escape
            if (e.key.startsWith('F') || e.key === 'Escape') {
                // No prevenimos default para dejar que el sistema use algunas
                dotNetHelper.invokeMethodAsync('HandleGlobalKey', e.key);
            }
        });

        window.addEventListener('mousedown', () => {
            this.tipsActive = false;
            document.body.classList.remove('uta-show-keys');
            document.body.classList.remove('uta-modal-open');
        });
    },
    hideTips: function() {
        this.tipsActive = false;
        document.body.classList.remove('uta-show-keys');
        document.body.classList.remove('uta-modal-open');
    }
};
