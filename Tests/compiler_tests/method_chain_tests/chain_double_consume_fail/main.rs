// f.consume() then f.consume() — double use-after-move on non-Copy.
struct Foo { val: i32 }
impl Foo { fn consume(self: Self) -> i32 { self.val } }
fn main() -> i32 {
    let f = make Foo { val: 42 };
    let a = f.consume();
    let b = f.consume();
    a + b
}
