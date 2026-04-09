fn main() -> i32 {
    let x = 10;
    let y = 20;
    let z = 10;
    let eq = x == z;
    let ne = x != y;
    let lt = x < y;
    let gt = y > x;
    let and = eq && ne;
    let or = lt || gt;
    let neg = !false;
    let result = 0;
    if eq { result = result + 1; };
    if ne { result = result + 2; };
    if lt { result = result + 4; };
    if gt { result = result + 8; };
    if and { result = result + 16; };
    if or { result = result + 32; };
    if neg { result = result + 64; };
    result
}
