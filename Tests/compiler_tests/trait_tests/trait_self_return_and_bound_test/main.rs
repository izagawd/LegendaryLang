trait Doubler: Sized {
    fn double(self: &Self, other: Self) -> Self;
}
impl Doubler for i32 {
    fn double(self: &Self, other: Self) -> Self {
        other + *self
    }
}
fn main() -> i32 {
    let x = 3;
    x.double(7)
}
