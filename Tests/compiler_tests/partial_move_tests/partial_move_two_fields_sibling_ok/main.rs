struct Inner { val: i32 }
struct Outer { a: Inner, b: Inner }
struct Deep { x: Outer, y: Inner }
fn main() -> i32 {
    let s = make Outer { a: make Inner { val: 20 }, b: make Inner { val: 22 } };
    let a = s.a;
    let b = s.b;
    a.val + b.val
}
