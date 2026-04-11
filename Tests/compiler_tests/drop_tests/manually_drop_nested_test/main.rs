use Std.Ops.Drop;
use Std.Mem.ManuallyDrop;

struct Inner['a] { r: &'a mut i32 }
impl['a] Drop for Inner['a] {
    fn Drop(self: &mut Self) { *self.r = *self.r + 1; }
}

struct Outer['a] { inner: Inner['a] }
impl['a] Drop for Outer['a] {
    fn Drop(self: &mut Self) { *self.inner.r = *self.inner.r + 10; }
}

fn main() -> i32 {
    let counter = 0;
    {
        let o = make Outer { inner: make Inner { r: &mut counter } };
        let _md = ManuallyDrop.New(o);
    };
    counter
}
