struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}

fn main() -> i32 {
    let inner = GcMut(Foo).New(make Foo { val: 42 });
    let outer = GcMut(GcMut(Foo)).New(inner);
    outer.get()
}
