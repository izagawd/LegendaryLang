struct Inner { val: i32 }
struct Outer { a: Inner, b: Inner }
struct Deep { x: Outer, y: Inner }
fn main() -> i32 {
    let s = make Deep { x: make Outer { a: make Inner { val: 0 }, b: make Inner { val: 0 } }, y: make Inner { val: 0 } };
    let moved = s.x.a;
    let whole_x = s.x;
    42
}
