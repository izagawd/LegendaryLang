fn main() -> i32 {
    let a = 3;
    let b = 7;
    let ra = &a;
    let rb = &b;
    let r1 = if ra < rb { 1 } else { 0 };
    let r2 = if rb > ra { 10 } else { 0 };
    let r3 = if ra <= rb { 100 } else { 0 };
    let r4 = if rb >= ra { 1000 } else { 0 };
    r1 + r2 + r3 + r4
}
