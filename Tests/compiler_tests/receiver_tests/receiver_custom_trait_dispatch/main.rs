// Trait method dispatched through custom Receiver. SmartPtr → Foo, Foo impls Greet.
use Std.Deref.Receiver;
use Std.Deref.Deref;

trait Greet {
    fn greet(self: &Self) -> i32;
}

struct Foo { val: i32 }

impl Greet for Foo {
    fn greet(self: &Self) -> i32 { self.val }
}

struct SmartPtr { data: Foo }

impl Receiver for SmartPtr { let Target :! type = Foo; }
impl Deref for SmartPtr {
    fn deref(self: &Self) -> &Foo { &self.data }
}

fn main() -> i32 {
    let w = make SmartPtr { data: make Foo { val: 42 } };
    w.greet()
}
