// Custom wrapper implementing Receiver+Deref. w.get() auto-derefs SmartPtr → Foo.
use Std.Deref.Receiver;
use Std.Deref.Deref;

struct Foo { val: i32 }

struct SmartPtr { data: Foo }

impl Receiver for SmartPtr { let Target :! type = Foo; }
impl Deref for SmartPtr {
    fn deref(self: &Self) -> &Foo {
        &self.data
    }
}

impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}

fn main() -> i32 {
    let w = make SmartPtr { data: make Foo { val: 42 } };
    w.get()
}
