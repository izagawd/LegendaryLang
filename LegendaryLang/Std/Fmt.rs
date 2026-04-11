use Std.Alloc.Gc;
use Std.Alloc.GcMut;
use Std.Deref.Deref;

trait ToString {
    fn ToString(self: &Self) -> &str;
}

fn PrintStr(input: &str);
fn PrintLnStr(input: &str);

fn Print[T:! ToString](input: &T) {
    let s: &str = input.ToString();
    PrintStr(s)
}

fn PrintLn[T:! ToString](input: &T) {
    let s: &str = input.ToString();
    PrintLnStr(s)
}

impl ToString for str {
    fn ToString(self: &Self) -> &str {
        self
    }
}

impl[T:! ToString] ToString for &T {
    fn ToString(self: &Self) -> &str {
        T.ToString(*self)
    }
}

impl[T:! ToString] ToString for &mut T {
    fn ToString(self: &Self) -> &str {
        T.ToString(*self)
    }
}

impl[T:! ToString] ToString for Gc(T) {
    fn ToString(self: &Self) -> &str {
        self.deref().ToString()
    }
}

impl[T:! ToString] ToString for GcMut(T) {
    fn ToString(self: &Self) -> &str {
        self.deref().ToString()
    }
}
