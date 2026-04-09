trait Maker: Sized {
    let Product :! type;
    fn make(x: i32) -> Self.Product;
}
impl Maker for i32 {
    let Product :! type = i32;
    fn make(x: i32) -> i32 {
        x
    }
}
fn main() -> i32 {
    let val: (i32 as Maker).Product = (i32 as Maker).make(42);
    val
}
