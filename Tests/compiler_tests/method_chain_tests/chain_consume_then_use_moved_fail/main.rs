// f.consume() moves f. Then f.get() — use after move.
struct Foo { val: i32 }
impl Foo {
    fn consume(self: Self) -> i32 { self.val }
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 42 };
    let v = f.consume();
    f.get() + v
}
