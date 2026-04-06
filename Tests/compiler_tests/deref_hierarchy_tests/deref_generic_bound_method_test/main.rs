use Std.Core.Deref.Deref;

struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}

fn call_through(T:! Deref(Target = Foo), t: &T) -> i32 {
    t.get()
}

fn main() -> i32 {
    let f = make Foo { val: 42 };
    let b = Box(Foo).New(f);
    call_through(Box(Foo), &b)
}
