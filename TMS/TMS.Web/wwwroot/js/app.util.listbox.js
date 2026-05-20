class ListBox {
    listBox;
    defaultOptions = {
        multiple: true,
        styling: false
    };
    options = {};

    constructor(listBox, options) {
        this.listBox = listBox;
        this.listBox.tabIndex = 0;
        this.options = { ...this.defaultOptions, ...options };

        if (this.options.styling) {
            const style = document.createElement("style");
            style.innerHTML = `.list-box{height:11rem;overflow:auto;border:2px solid rgb(32 57 112 / 10%);border-radius:.5rem;backdrop-filter:blur(1rem);background-color:#f4f4f4;padding:5px;}.list-box .list-item{padding:.2rem .5rem;user-select:none;background-color:#ffffff;margin:0px 0px 4px 0px;border-radius:5px;}.list-box>[selected]{background-color:#203970;color:#fff}.list-box>[selected][focused]{outline:rgba(0,0,0,.2) solid 1px;outline-offset:-1px}`;
            document.head.appendChild(style);
        }

        listBox.onclick = (e) => {
            e.preventDefault();
            if (e.target.parentElement == listBox) {
                if (e.shiftKey) {
                    if (this.focusedChild) {
                        const [start, end] = this.getRangeFromLastSelected(e.target);
                        this.selectRange(start, end);
                        this.focusChild(e.target);
                    }
                } else {
                    this.toggleSelect(e.target);
                }
            }
            this.listBox.dispatchEvent(
                new CustomEvent("clicked", { detail: { e: e, object: this } })
            );
        };

        listBox.onkeydown = (e) => {
            e.preventDefault();
            if (e.key == "ArrowUp") {
                e.shiftKey && this.toggleSelect();
                this.focusPrevious();
            }
            if (e.key == "ArrowDown") {
                e.shiftKey && this.toggleSelect();
                this.focusNext();
            }
            if (e.key == "Enter" || e.code == "Space") {
                this.toggleSelect();
            }
            if (this.options.multiple) {
                if (e.ctrlKey && e.key == "a") {
                    this.toggleAll();
                }
            }
            this.listBox.dispatchEvent(
                new CustomEvent("changed", { detail: { e: e, object: this } })
            );
        };
    }

    get focusedChild() {
        return [...this.listBox.children].find((c) => c.hasAttribute("focused"));
    }

    get isAllSelected() {
        return [...this.listBox.children].every((child) =>
            child.hasAttribute("selected")
        );
    }

    get value() {
        return [...this.listBox.children]
            .filter((child) => child.hasAttribute("selected"))
            .map((child) => child.value || child.textContent);
    }

    selectAll() {
        [...this.listBox.children].forEach((child) =>
            this.toggleSelect(child, true)
        );
        this.scrollToFocus();
    }

    clearAll() {
        [...this.listBox.children].forEach((child) =>
            this.toggleSelect(child, false)
        );
        this.focusFirst();
        this.scrollToFocus();
    }

    toggleAll() {
        this.isAllSelected ? this.clearAll() : this.selectAll();
    }

    focusFirst() {
        this.focusChild(this.listBox.firstElementChild);
    }

    focusNext() {
        if (!this.focusedChild) {
            this.focusFirst();
        } else {
            this.focusedChild.nextElementSibling &&
                this.focusChild(this.focusedChild.nextElementSibling);
        }
        this.scrollToFocus();
    }

    focusPrevious(child) {
        if (!this.focusedChild) {
            this.focusLast();
        } else {
            this.focusedChild.previousElementSibling &&
                this.focusChild(this.focusedChild.previousElementSibling);
        }
        this.scrollToFocus();
    }

    focusLast() {
        this.focusChild(this.listBox.lastElementChild);
    }

    toggleSelect(child, value) {
        if (!child) {
            if (!this.focusedChild) {
                this.focusFirst();
            }
            child = this.focusedChild;
        }
        const condition =
            value != undefined ? value : !child.hasAttribute("selected");
        var isDisabled = child.hasAttribute("disabled");
        if (!isDisabled) {
            if (condition) {
                if (this.options.multiple) {
                    child.setAttribute("selected", "");
                } else {
                    this.selectOnlyOneChild(child);
                }
            } else {
                child.removeAttribute("selected");
            }
            this.focusChild(child);
        }
    }

    selectOnlyOneChild(child) {
        [...this.listBox.children].forEach((c) => {
            if (c == child) {
                c.setAttribute("selected", "");
            } else {
                c.removeAttribute("selected");
            }
        });
    }

    focusChild(child) {
        [...this.listBox.children].forEach((c) => {
            if (c == child) {
                c.setAttribute("focused", "");
            } else {
                c.removeAttribute("focused");
            }
        });
    }

    selectRange(from, to) {
        const children = [...this.listBox.children];
        for (let i = from; i <= to; i++) {
            this.toggleSelect(
                children[i],
                this.focusedChild.hasAttribute("selected")
            );
        }
    }

    getRangeFromLastSelected(child) {
        const f = this.getIndex(this.focusedChild);
        const c = this.getIndex(child);
        const start = f < c ? f : c;
        const end = f > c ? f : c;
        return [start, end];
    }

    getIndex(child) {
        return [...this.listBox.children].indexOf(child);
    }

    scrollToFocus() {
        this.listBox.scrollTop =
            this.focusedChild.offsetTop - this.listBox.clientHeight / 2;
    }
}