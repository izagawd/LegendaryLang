fn main() -> i32 {
    let a = 5;
    let b = 10;
    let c = 5;
    let result = 0;
    if a == c { result = result + 1; };
    if a != b { result = result + 2; };
    if a < b  { result = result + 4; };
    if b > a  { result = result + 8; };
    if a <= c { result = result + 16; };
    if a <= b { result = result + 32; };
    if b >= a { result = result + 64; };
    if c >= a { result = result + 128; };
    result
}
