// Custom wrapper with DerefMut. w.set(42) auto-derefs → &mut Foo via deref_mut.
use Std.Deref.Receiver;
use Std.Deref.Deref;
use Std.Deref.DerefMut;

struct Foo { val: i32 }

struct MutPtr { data: Foo }

impl Receiver for MutPtr { let Target :! type = Foo; }
impl Deref for MutPtr {
    fn deref(self: &Self) -> &Foo { &self.data }
}
impl DerefMut for MutPtr {
    fn deref_mut(self: &mut Self) -> &mut Foo { &mut self.data }
}

impl Foo {
    fn set(self: &mut Self, v: i32) { self.val = v; }
    fn get(self: &Self) -> i32 { self.val }
}

fn main() -> i32 {
    let w = make MutPtr { data: make Foo { val: 0 } };
    w.set(42);
    w.get()
}
