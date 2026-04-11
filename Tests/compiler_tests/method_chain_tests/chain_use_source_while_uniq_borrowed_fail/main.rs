// Wrapper borrows &mut dropped. While vald (which holds &mut dropped) is live,
// using dropped directly is a use-while-borrowed error.
use Std.Ops.Drop;
use Std.Deref.Receiver;
use Std.Deref.Deref;

struct Wrapper['a] {
    inner: Gc(i32),
    dropped: &'a mut i32
}

impl['a] Wrapper['a] {
    fn New(dropped: &'a mut i32, val: i32) -> Wrapper['a] {
        make Wrapper { inner: Gc.New(val), dropped: dropped }
    }
}

impl['a] Receiver for Wrapper['a] { let Target:! Sized = i32; }
impl['a] Deref for Wrapper['a] {
    fn deref(self: &Self) -> &i32 { &*self.inner }
}

impl['a] Drop for Wrapper['a] {
    fn Drop(self: &mut Self) {
        *self.dropped = *self.dropped + 1;
    }
}

fn Run() -> i32 {
    let dropped = 0;
    let vald = Wrapper.New(&mut dropped, 7);
    let val = &*vald;
    *val + dropped
}

fn main() -> i32 { Run() }
