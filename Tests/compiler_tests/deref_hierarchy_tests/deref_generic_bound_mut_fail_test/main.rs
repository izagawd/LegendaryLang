use Std.Deref.Deref;

struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn need_mut(self: &mut Self) -> i32 { self.val }
}

fn call_through(T:! Deref(Target = Foo), t: &T) -> i32 {
    t.need_mut()
}

fn main() -> i32 {
    let f = make Foo { val: 5 };
    let b = Box(Foo).New(f);
    call_through(Box(Foo), &b)
}
