// Builder pattern but try to use intermediate after it's been consumed.
// b.set_x(20) consumes b. Then b.set_y(22) is use-after-move.
struct Builder { x: i32, y: i32 }
impl Builder {
    fn set_x(self: Self, v: i32) -> Builder { make Builder { x: v, y: self.y } }
    fn set_y(self: Self, v: i32) -> Builder { make Builder { x: self.x, y: v } }
}
fn main() -> i32 {
    let b = make Builder { x: 0, y: 0 };
    let b2 = b.set_x(20);
    let b3 = b.set_y(22);
    b2.x + b3.y
}
