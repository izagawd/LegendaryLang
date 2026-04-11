struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}
fn take_box(b: &GcMut(Foo)) -> i32 {
    b.get()
}
fn main() -> i32 {
    let f = make Foo { val: 42 };
    let b = GcMut(Foo).New(f);
    take_box(&b)
}
