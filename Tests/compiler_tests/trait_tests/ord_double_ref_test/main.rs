fn main() -> i32 {
    let a = 3;
    let b = 7;
    let ra = &a;
    let rb = &b;
    let rra = &ra;
    let rrb = &rb;
    let r1 = if rra < rrb { 1 } else { 0 };
    let r2 = if rrb > rra { 10 } else { 0 };
    r1 + r2
}
