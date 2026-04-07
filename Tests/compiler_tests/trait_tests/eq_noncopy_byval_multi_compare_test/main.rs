use Std.Ops.PartialEq;

struct Wrapper { val: i32 }

impl PartialEq(Wrapper) for Wrapper {
    fn Eq(lhs: Wrapper, rhs: Wrapper) -> bool {
        lhs.val == rhs.val
    }
}

fn main() -> i32 {
    let a = make Wrapper { val: 10 };
    let b = make Wrapper { val: 10 };
    let c = make Wrapper { val: 20 };
    let r1 = a == b;
    let r2 = a == c;
    let r3 = a != c;
    let result = 0;
    if r1 { result = result + 1; };
    if r2 { result = result + 10; };
    if r3 { result = result + 100; };
    result
}
