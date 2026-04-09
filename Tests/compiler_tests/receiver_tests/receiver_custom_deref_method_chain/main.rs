// Custom wrapper: w.get_ref().val — auto-deref SmartPtr → Foo, method returns &i32, field access.
use Std.Deref.Receiver;
use Std.Deref.Deref;

struct Foo { x: i32, y: i32 }

struct SmartPtr { data: Foo }

impl Receiver for SmartPtr { let Target :! type = Foo; }
impl Deref for SmartPtr {
    fn deref(self: &Self) -> &Foo { &self.data }
}

impl Foo {
    fn sum(self: &Self) -> i32 { self.x + self.y }
}

fn main() -> i32 {
    let w = make SmartPtr { data: make Foo { x: 20, y: 22 } };
    w.sum()
}
