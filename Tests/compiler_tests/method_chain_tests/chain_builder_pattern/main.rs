// Builder pattern: b.set_x(20).set_y(22) — two-level chain, both self:Self.
// Then .x + .y on the result.
struct Builder { x: i32, y: i32 }
impl Builder {
    fn set_x(self: Self, v: i32) -> Builder { make Builder { x: v, y: self.y } }
    fn set_y(self: Self, v: i32) -> Builder { make Builder { x: self.x, y: v } }
}
fn main() -> i32 {
    let b = make Builder { x: 0, y: 0 };
    let result = b.set_x(20).set_y(22);
    result.x + result.y
}
